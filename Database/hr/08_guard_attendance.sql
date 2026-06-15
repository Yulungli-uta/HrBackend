/*
  HR.sp_ProcessGuardAttendanceDate
  =================================
  Calcula la asistencia del día para todos los guardias que tienen un turno
  planificado activo en GuardShiftPlanning.

  Diferencias vs el pipeline normal (sp_ProcessAttendanceRunDate):
    - El horario del día se resuelve desde GuardShiftPlanning, NO desde EmployeeSchedules.
    - Si existe un GuardShiftChange activo (IsActiveForAttendance=1) el cálculo se
      ejecuta sobre el empleado de REEMPLAZO con el NewScheduleId (si hay cambio de
      horario) o con el horario original del turno.
    - Al finalizar el cálculo se actualizan las columnas específicas de guardias en
      tbl_AttendanceCalculations: GuardShiftPlanningID, GuardShiftChangeID,
      OriginalEmployeeID, EffectiveEmployeeID, IsReplacement.
    - El estado del GuardShiftPlanning se actualiza a COMPLETED o ABSENT según
      si hubo picadas válidas.

  Anti-duplicado:
    sp_ProcessAttendanceRunDate excluye del loop a todos los empleados que tienen
    al menos un GuardShiftPlanning activo en la fecha, por lo que un guardia
    que coincidentalmente tenga EmployeeSchedule nunca se procesa dos veces.
*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessGuardAttendanceDate
(
    @WorkDate DATE,
    @Debug    BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @WorkDate IS NULL
        THROW 50001, 'El parametro @WorkDate es obligatorio.', 1;

    /* =========================================================
       1. PARÁMETROS DEL SISTEMA — igual que el SP general
       ========================================================= */
    DECLARE
        @GraceMin   INT  = 0,
        @OTMin      INT  = 0,
        @NightStart TIME = NULL,
        @NightEnd   TIME = NULL,
        @IsHoliday  BIT  = 0,
        @IsWeekend  BIT  = 0;

    SELECT
        @GraceMin   = ISNULL(MAX(CASE WHEN name='TARDINESS_GRACE_MIN'   THEN TRY_CAST(Pvalues AS INT)  END), 0),
        @OTMin      = ISNULL(MAX(CASE WHEN name='OT_MIN_THRESHOLD_MIN'  THEN TRY_CAST(Pvalues AS INT)  END), 0),
        @NightStart =        MAX(CASE WHEN name='NIGHT_START'           THEN TRY_CAST(Pvalues AS TIME) END),
        @NightEnd   =        MAX(CASE WHEN name='NIGHT_END'             THEN TRY_CAST(Pvalues AS TIME) END)
    FROM HR.tbl_Parameters
    WHERE name IN ('TARDINESS_GRACE_MIN','OT_MIN_THRESHOLD_MIN','NIGHT_START','NIGHT_END');

    IF @NightStart IS NULL SET @NightStart = CAST('22:00:00' AS TIME);
    IF @NightEnd   IS NULL SET @NightEnd   = CAST('06:00:00' AS TIME);

    SELECT
        @IsHoliday = ISNULL(IsHoliday, 0),
        @IsWeekend = ISNULL(IsWeekend, 0)
    FROM HR.vw_Calendar
    WHERE D = @WorkDate;

    /* =========================================================
       2. RefType IDs para actualizar estados de planificación
       ========================================================= */
    DECLARE
        @StatusCompleted INT,
        @StatusAbsent    INT;

    SELECT @StatusCompleted = TypeId FROM HR.ref_Types
    WHERE Category = 'GUARD_PLANNING_STATUS' AND Name = 'COMPLETED';

    SELECT @StatusAbsent = TypeId FROM HR.ref_Types
    WHERE Category = 'GUARD_PLANNING_STATUS' AND Name = 'ABSENT';

    /* =========================================================
       3. TURNOS DEL DÍA: GuardShiftPlanning activos + cambio
          activo si existe
       ========================================================= */
    DROP TABLE IF EXISTS #GuardShifts;

    SELECT
        gsp.PlanningId,
        gsp.EmployeeId                                            AS OriginalEmployeeId,
        -- Si hay cambio activo, el que trabaja es el reemplazo
        ISNULL(gsc.ReplacementEmployeeId, gsp.EmployeeId)        AS EffectiveEmployeeId,
        -- Horario: si el cambio tiene NewScheduleId se usa ese, si no el del turno
        ISNULL(gsc.NewScheduleId, gsp.ScheduleId)                AS EffectiveScheduleId,
        CASE WHEN gsc.ShiftChangeId IS NOT NULL THEN 1 ELSE 0 END AS IsReplacement,
        gsc.ShiftChangeId,
        s.EntryTime,
        s.ExitTime,
        s.HasLunchBreak,
        s.LunchStart,
        s.LunchEnd,
        ved.ContractType,
        ROW_NUMBER() OVER (ORDER BY gsp.PlanningId) AS RowNum
    INTO #GuardShifts
    FROM HR.tbl_GuardShiftPlanning gsp
    -- Cambio activo para este turno (máximo 1 por turno)
    LEFT JOIN HR.tbl_GuardShiftChanges gsc
        ON  gsc.PlanningId           = gsp.PlanningId
        AND gsc.IsActiveForAttendance = 1
    -- Horario efectivo
    JOIN HR.tbl_Schedules s
        ON s.ScheduleID = ISNULL(gsc.NewScheduleId, gsp.ScheduleId)
    -- Tipo de contrato del empleado efectivo (para subsidio alimentación)
    LEFT JOIN HR.vw_EmployeeDetails ved
        ON ved.EmployeeID = ISNULL(gsc.ReplacementEmployeeId, gsp.EmployeeId)
    WHERE gsp.WorkDate            = @WorkDate
      AND gsp.IsActiveForAssignment = 1;

    IF NOT EXISTS (SELECT 1 FROM #GuardShifts)
    BEGIN
        DROP TABLE IF EXISTS #GuardShifts;
        RETURN;
    END;

    /* =========================================================
       4. LOOP: procesar cada turno
       ========================================================= */
    DECLARE
        @MaxRow           INT,
        @Row              INT = 1,
        @PlanningId       INT,
        @OriginalEmpId    INT,
        @EffectiveEmpId   INT,
        @EffectiveSchedId INT,
        @IsRepl           BIT,
        @ShiftChangeId    INT,
        @EntryTime        TIME,
        @ExitTime         TIME,
        @HasLunch         BIT,
        @LunchStartT      TIME,
        @LunchEndT        TIME,
        @ContractType     NVARCHAR(100);

    SELECT @MaxRow = MAX(RowNum) FROM #GuardShifts;
    IF @MaxRow IS NULL SET @MaxRow = 0;

    WHILE @Row <= @MaxRow
    BEGIN
        SELECT
            @PlanningId       = PlanningId,
            @OriginalEmpId    = OriginalEmployeeId,
            @EffectiveEmpId   = EffectiveEmployeeId,
            @EffectiveSchedId = EffectiveScheduleId,
            @IsRepl           = IsReplacement,
            @ShiftChangeId    = ShiftChangeId,
            @EntryTime        = EntryTime,
            @ExitTime         = ExitTime,
            @HasLunch         = HasLunchBreak,
            @LunchStartT      = LunchStart,
            @LunchEndT        = LunchEnd,
            @ContractType     = ContractType
        FROM #GuardShifts WHERE RowNum = @Row;

        BEGIN TRY
            /* 4a. Calcular asistencia base usando el horario del turno rotativo */
            EXEC HR.sp_ProcessAttendanceBaseDay
                @EmployeeID   = @EffectiveEmpId,
                @WorkDate     = @WorkDate,
                @GraceMin     = @GraceMin,
                @OTMin        = @OTMin,
                @NightStart   = @NightStart,
                @NightEnd     = @NightEnd,
                @ContractType = @ContractType,
                @IsHoliday    = @IsHoliday,
                @IsWeekend    = @IsWeekend,
                @ScheduleID   = @EffectiveSchedId,
                @EntryTime    = @EntryTime,
                @ExitTime     = @ExitTime,
                @HasLunch     = @HasLunch,
                @LunchStartT  = @LunchStartT,
                @LunchEndT    = @LunchEndT;

            /* 4b. Aplicar novedades: permisos, vacaciones, justificaciones, recuperación */
            EXEC HR.sp_ProcessAttendanceLeavesDay
                @EmployeeID = @EffectiveEmpId,
                @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendanceJustificationsDay
                @EmployeeID = @EffectiveEmpId,
                @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendanceRecoveryDay
                @EmployeeID = @EffectiveEmpId,
                @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendanceFinalizeDay
                @EmployeeID   = @EffectiveEmpId,
                @WorkDate     = @WorkDate,
                @ContractType = @ContractType;

            /* 4c. Anotar los campos específicos de guardias en el registro de cálculo */
            UPDATE HR.tbl_AttendanceCalculations
            SET
                GuardShiftPlanningID = @PlanningId,
                GuardShiftChangeID   = @ShiftChangeId,
                OriginalEmployeeID   = @OriginalEmpId,
                EffectiveEmployeeID  = @EffectiveEmpId,
                IsReplacement        = @IsRepl
            WHERE EmployeeID = @EffectiveEmpId
              AND WorkDate   = @WorkDate;

            /* 4d. Actualizar estado del turno:
                   COMPLETED  si hay al menos una picada válida (TotalWorkedMinutes > 0)
                   ABSENT     si no hubo picadas                                       */
            DECLARE @TotalWorked INT = 0;
            SELECT @TotalWorked = ISNULL(TotalWorkedMinutes, 0)
            FROM HR.tbl_AttendanceCalculations
            WHERE EmployeeID = @EffectiveEmpId AND WorkDate = @WorkDate;

            UPDATE HR.tbl_GuardShiftPlanning
            SET StatusTypeId = CASE WHEN @TotalWorked > 0 THEN @StatusCompleted ELSE @StatusAbsent END,
                UpdatedAt    = GETDATE()
            WHERE PlanningId = @PlanningId;

        END TRY
        BEGIN CATCH
            DECLARE @ErrMsg   NVARCHAR(4000) = ERROR_MESSAGE();
            DECLARE @ThrowMsg NVARCHAR(4000) =
                'Error procesando guardia PlanningId=' + CAST(@PlanningId AS VARCHAR)
                + ' EmployeeId=' + CAST(@EffectiveEmpId AS VARCHAR)
                + ' Fecha=' + CONVERT(VARCHAR(10), @WorkDate, 120)
                + ' -> ' + ISNULL(@ErrMsg, 'Error desconocido.');

            IF @Debug = 1
                PRINT @ThrowMsg;
            ELSE
                THROW 50020, @ThrowMsg, 1;
        END CATCH;

        SET @Row += 1;
    END;

    DROP TABLE IF EXISTS #GuardShifts;
END;
GO
