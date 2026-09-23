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

  2026-09-21: sincronizado con la definición real en producción (este archivo había
  quedado desactualizado desde el fix de turno doble del 2026-09-09 — nunca pasaba
  @JourneyNumber a los sub-SP, pese a que el comentario de 21_journey_number_and_
  special_schedules.sql decía que sí lo hacía). Además se amplía el cálculo de
  turno vecino (PrevShiftEndDT/NextShiftStartDT) para que también considere el
  último turno del día calendario anterior y el primero del día siguiente, no solo
  los turnos del mismo @WorkDate — antes, un turno nocturno que termina de
  madrugada del día siguiente y un turno que empieza esa misma tarde no se veían
  entre sí (cada @WorkDate se procesa en una corrida aislada de este SP), así que
  sus ventanas de captura de picadas (+/-4h) podían traslaparse sin ningún tope.
*/
-- Fase 4 (2026-07-03): forzar sesión correcta antes de compilar el SP, mismo
-- motivo documentado en 06_procedures.sql (evita el error 1934/QUOTED_IDENTIFIER
-- si quien despliega este archivo tiene una sesión con la config incorrecta).
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE HR.sp_ProcessGuardAttendanceDate
(
    @WorkDate DATE,
    @Debug    BIT = 0,
    -- 2026-07-06: filtro opcional. NULL = comportamiento actual (todos los
    -- guardias con turno activo ese día). Con valor, acota el reproceso al
    -- guardia indicado (titular o reemplazo). Evita que un reproceso acotado
    -- por empleado en sp_ProcessAttendanceRunDate termine tocando a todos
    -- los guardias de la fecha.
    @FilterEmployeeID INT = NULL
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
          activo si existe.
          2026-09-09: turno doble — un mismo guardia (EffectiveEmployeeId)
          puede tener 2+ turnos el mismo WorkDate (ej. grupo especial
          mañana+noche). Se numera cada turno (JourneyNumber, por hora de
          inicio) y se calcula el inicio/fin del turno VECINO inmediato
          (anterior/siguiente) para capar la ventana de captura de picadas
          de sp_ProcessAttendanceBaseDay y que no se traslape con el turno
          vecino (las ventanas de +/-4h sí pueden traslaparse aunque los
          turnos reales no).
          2026-09-21: RawWide trae turnos de WorkDate-1..WorkDate+1 (no solo
          @WorkDate) únicamente para que Prev/NextShift también vean turnos
          de días calendario vecinos (ej. turno nocturno que cruza medianoche
          seguido de un turno esa misma tarde del día siguiente). JourneyNumber
          y RowNum se calculan DESPUÉS de filtrar a WorkDate=@WorkDate, así que
          siguen numerando solo los turnos del día que se está procesando —
          nada cambia para el caso normal de un único turno por día.
       ========================================================= */
    DROP TABLE IF EXISTS #GuardShifts;

    ;WITH RawWide AS
    (
        SELECT
            gsp.PlanningId,
            gsp.WorkDate,
            gsp.EmployeeId                                            AS OriginalEmployeeId,
            -- Si hay cambio activo, el que trabaja es el reemplazo
            ISNULL(gsc.ReplacementEmployeeId, gsp.EmployeeId)        AS EffectiveEmployeeId,
            -- Horario: si el cambio tiene NewScheduleId se usa ese, si no el del turno
            ISNULL(gsc.NewScheduleId, gsp.ScheduleId)                AS EffectiveScheduleId,
            -- 2026-09-09: antes marcaba IsReplacement=1 con CUALQUIER cambio activo,
            -- incluida una REASSIGNMENT (mismo guardia, solo cambia día/horario/ubicación).
            -- Ahora solo es "reemplazo" si el cambio trae un empleado distinto cubriendo.
            CASE WHEN gsc.ReplacementEmployeeId IS NOT NULL THEN 1 ELSE 0 END AS IsReplacement,
            gsc.ShiftChangeId,
            s.EntryTime,
            s.ExitTime,
            s.HasLunchBreak,
            s.LunchStart,
            s.LunchEnd,
            ved.ContractType,
            DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), s.EntryTime), CAST(gsp.WorkDate AS DATETIME2)) AS ShiftStartDT,
            CASE WHEN s.ExitTime <= s.EntryTime
                 THEN DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), s.ExitTime), DATEADD(DAY, 1, CAST(gsp.WorkDate AS DATETIME2)))
                 ELSE DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), s.ExitTime), CAST(gsp.WorkDate AS DATETIME2))
            END AS ShiftEndDT
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
        -- 2026-09-21: antes era "WHERE gsp.WorkDate = @WorkDate" — se amplía a
        -- +/-1 día calendario solo para poder calcular PrevShiftEndDT/NextShiftStartDT
        -- correctamente en los bordes del día procesado (ver comentario arriba).
        WHERE gsp.WorkDate BETWEEN DATEADD(DAY, -1, @WorkDate) AND DATEADD(DAY, 1, @WorkDate)
          AND gsp.IsActiveForAssignment = 1
          AND (@FilterEmployeeID IS NULL
               OR gsp.EmployeeId = @FilterEmployeeID
               OR ISNULL(gsc.ReplacementEmployeeId, gsp.EmployeeId) = @FilterEmployeeID)
    ),
    WithNeighbors AS
    (
        SELECT
            *,
            LAG(ShiftEndDT)    OVER (PARTITION BY EffectiveEmployeeId ORDER BY ShiftStartDT, PlanningId) AS PrevShiftEndDT,
            LEAD(ShiftStartDT) OVER (PARTITION BY EffectiveEmployeeId ORDER BY ShiftStartDT, PlanningId) AS NextShiftStartDT
        FROM RawWide
    )
    SELECT
        *,
        ROW_NUMBER() OVER (PARTITION BY EffectiveEmployeeId ORDER BY ShiftStartDT, PlanningId) AS JourneyNumber,
        ROW_NUMBER() OVER (ORDER BY PlanningId) AS RowNum
    INTO #GuardShifts
    FROM WithNeighbors
    WHERE WorkDate = @WorkDate;

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
        @ContractType     NVARCHAR(100),
        @JourneyNumber    INT,
        @WindowStartCap   DATETIME2,
        @WindowEndCap     DATETIME2;

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
            @ContractType     = ContractType,
            @JourneyNumber    = JourneyNumber,
            @WindowStartCap   = PrevShiftEndDT,
            @WindowEndCap     = NextShiftStartDT
        FROM #GuardShifts WHERE RowNum = @Row;

        BEGIN TRY
            /* 4a. Calcular asistencia base usando el horario del turno rotativo */
            EXEC HR.sp_ProcessAttendanceBaseDay
                @EmployeeID      = @EffectiveEmpId,
                @WorkDate        = @WorkDate,
                @GraceMin        = @GraceMin,
                @OTMin           = @OTMin,
                @NightStart      = @NightStart,
                @NightEnd        = @NightEnd,
                @ContractType    = @ContractType,
                @IsHoliday       = @IsHoliday,
                @IsWeekend       = @IsWeekend,
                @ScheduleID      = @EffectiveSchedId,
                @EntryTime       = @EntryTime,
                @ExitTime        = @ExitTime,
                @HasLunch        = @HasLunch,
                @LunchStartT     = @LunchStartT,
                @LunchEndT       = @LunchEndT,
                @JourneyNumber   = @JourneyNumber,
                @WindowStartCap  = @WindowStartCap,
                @WindowEndCap    = @WindowEndCap;

            /* 4b. Aplicar novedades: permisos, vacaciones, justificaciones, recuperación */
            EXEC HR.sp_ProcessAttendanceLeavesDay
                @EmployeeID = @EffectiveEmpId,
                @WorkDate   = @WorkDate,
                @JourneyNumber = @JourneyNumber;

            EXEC HR.sp_ProcessAttendanceJustificationsDay
                @EmployeeID = @EffectiveEmpId,
                @WorkDate   = @WorkDate,
                @JourneyNumber = @JourneyNumber;

            EXEC HR.sp_ProcessAttendanceRecoveryDay
                @EmployeeID = @EffectiveEmpId,
                @WorkDate   = @WorkDate,
                @JourneyNumber = @JourneyNumber;

            /* 4b-bis. Fase 4 punto 4.5: consolidar horas extra/recuperación
               planificadas hacia HR.tbl_Overtime. Antes de este fix, los
               guardias nunca pasaban por este paso (el pipeline normal sí lo
               hace vía sp_ProcessAttendanceRunDate) y su horas extra
               ejecutadas jamás llegaban a facturarse. Se le pasa el horario
               ya resuelto del turno (@EntryTime/@ExitTime) porque
               sp_ProcessTimePlanningForEmployeeDay resuelve por defecto
               contra tbl_EmployeeSchedules, tabla que los guardias no usan. */
            EXEC HR.sp_ProcessAttendancePlanningDay
                @EmployeeID        = @EffectiveEmpId,
                @WorkDate          = @WorkDate,
                @Debug             = @Debug,
                @OverrideEntryTime = @EntryTime,
                @OverrideExitTime  = @ExitTime,
                @JourneyNumber     = @JourneyNumber;

            EXEC HR.sp_ProcessAttendanceFinalizeDay
                @EmployeeID   = @EffectiveEmpId,
                @WorkDate     = @WorkDate,
                @ContractType = @ContractType,
                @JourneyNumber = @JourneyNumber;

            /* 4c. Anotar los campos específicos de guardias en el registro de cálculo */
            UPDATE HR.tbl_AttendanceCalculations
            SET
                GuardShiftPlanningID = @PlanningId,
                GuardShiftChangeID   = @ShiftChangeId,
                OriginalEmployeeID   = @OriginalEmpId,
                EffectiveEmployeeID  = @EffectiveEmpId,
                IsReplacement        = @IsRepl
            WHERE EmployeeID = @EffectiveEmpId
              AND WorkDate   = @WorkDate
              AND JourneyNumber = @JourneyNumber;

            /* 4d. Actualizar estado del turno:
                   COMPLETED  si hay al menos una picada válida (TotalWorkedMinutes > 0)
                   ABSENT     si no hubo picadas
               2026-07-06: solo se evalúa si @WorkDate ya pasó (hoy o antes).
               Antes, reprocesar una fecha futura sin marcaciones (el turno
               todavía no ocurre) marcaba ABSENT a un guardia que ni siquiera
               ha llegado su turno — confirmado con la prueba controlada de
               PlanEmployeeID, donde 50 guardias reales quedaron ABSENT por
               error al reprocesar 2026-07-15 antes de que llegara la fecha.
               2026-09-09: el SELECT ahora filtra también por JourneyNumber
               — con turno doble, sin este filtro el SELECT era ambiguo entre
               las 2 filas del día y podía tomar el TotalWorked de la jornada
               equivocada. */
            IF @WorkDate <= CAST(GETDATE() AS DATE)
            BEGIN
                DECLARE @TotalWorked INT = 0;
                SELECT @TotalWorked = ISNULL(TotalWorkedMinutes, 0)
                FROM HR.tbl_AttendanceCalculations
                WHERE EmployeeID = @EffectiveEmpId AND WorkDate = @WorkDate AND JourneyNumber = @JourneyNumber;

                UPDATE HR.tbl_GuardShiftPlanning
                SET StatusTypeId = CASE WHEN @TotalWorked > 0 THEN @StatusCompleted ELSE @StatusAbsent END,
                    UpdatedAt    = GETDATE()
                WHERE PlanningId = @PlanningId;
            END

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
