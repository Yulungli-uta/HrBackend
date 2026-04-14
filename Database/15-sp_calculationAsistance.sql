/*-----  HR.sp_ProcessAttendanceBaseDay -*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceBaseDay
(
    @EmployeeID INT,
    @WorkDate DATE,
    @GraceMin INT,
    @OTMin INT,
    @NightStart TIME,
    @NightEnd TIME,
    @ContractType NVARCHAR(100) = NULL,
    @IsHoliday BIT = 0,
    @IsWeekend BIT = 0,
    @ScheduleID INT = NULL,
    @EntryTime TIME = NULL,
    @ExitTime TIME = NULL,
    @HasLunch BIT = NULL,
    @LunchStartT TIME = NULL,
    @LunchEndT TIME = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceBaseDay
      DESCRIPCIÓN:
          Calcula la asistencia base de un empleado en una fecha específica.

      RESPONSABILIDADES:
          - Obtener horario vigente del empleado
          - Obtener marcaciones del día
          - Calcular minutos trabajados
          - Calcular minutos dentro y fuera del horario
          - Calcular atrasos bruto/neto
          - Calcular ausencia, salida anticipada, nocturnidad y feriado
          - Persistir snapshot del horario aplicado
          - Crear/actualizar HR.tbl_AttendanceCalculations

      NOTAS:
          - No aplica permisos, vacaciones ni justificaciones.
          - No calcula recovery ni planning.
          - Los minutos de gracia se leen desde HR.tbl_Parameters.
    **********************************************************************/

    /* 
       Si el horario ya fue precargado por HR.sp_ProcessAttendanceRunDate,
       se usa directamente. Si no, se hace fallback a la consulta original.
    */
    IF @ScheduleID IS NULL
    BEGIN
        SELECT TOP 1
            @ScheduleID  = es.ScheduleID,
            @EntryTime   = s.EntryTime,
            @ExitTime    = s.ExitTime,
            @HasLunch    = s.HasLunchBreak,
            @LunchStartT = s.LunchStart,
            @LunchEndT   = s.LunchEnd
        FROM HR.tbl_EmployeeSchedules es
        INNER JOIN HR.tbl_Schedules s
            ON s.ScheduleID = es.ScheduleID
        WHERE es.EmployeeID = @EmployeeID
          AND es.ValidFrom <= @WorkDate
          AND (es.ValidTo IS NULL OR es.ValidTo >= @WorkDate)
        ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC;
    END;

    IF @ScheduleID IS NULL
        RETURN;

    DECLARE
        @BaseDate      DATETIME2 = CAST(@WorkDate AS DATETIME2),
        @ShiftStart    DATETIME2,
        @ShiftEnd      DATETIME2,
        @LunchStart    DATETIME2 = NULL,
        @LunchEnd      DATETIME2 = NULL,
        @NightStartDT  DATETIME2 = NULL,
        @NightEndDT    DATETIME2 = NULL,
        @RequiredMin   INT       = 0;

    SET @ShiftStart = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @EntryTime), @BaseDate);

    IF (@ExitTime <= @EntryTime)
        SET @ShiftEnd = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime), DATEADD(DAY, 1, @BaseDate));
    ELSE
        SET @ShiftEnd = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime), @BaseDate);

    IF (@HasLunch = 1 AND @LunchStartT IS NOT NULL AND @LunchEndT IS NOT NULL)
    BEGIN
        SET @LunchStart = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchStartT), @BaseDate);
        SET @LunchEnd   = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchEndT), @BaseDate);

        IF (@LunchEndT <= @LunchStartT)
            SET @LunchEnd = DATEADD(DAY, 1, @LunchEnd);
    END;

    IF (@NightStart IS NOT NULL AND @NightEnd IS NOT NULL)
    BEGIN
        SET @NightStartDT = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightStart), @BaseDate);

        IF (@NightEnd > @NightStart)
            SET @NightEndDT = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightEnd), @BaseDate);
        ELSE
            SET @NightEndDT = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightEnd), DATEADD(DAY, 1, @BaseDate));
    END;

    SET @RequiredMin = DATEDIFF(MINUTE, @ShiftStart, @ShiftEnd)
                     - CASE
                           WHEN @HasLunch = 1 AND @LunchStart IS NOT NULL AND @LunchEnd IS NOT NULL
                               THEN DATEDIFF(MINUTE, @LunchStart, @LunchEnd)
                           ELSE 0
                       END;

    IF @RequiredMin < 0 SET @RequiredMin = 0;

    DECLARE
        @WindowStart DATETIME2 = DATEADD(HOUR, -4, @ShiftStart),
        @WindowEnd   DATETIME2 = DATEADD(HOUR,  4, @ShiftEnd);

    DROP TABLE IF EXISTS #Punches;
    DROP TABLE IF EXISTS #Segments;

    ;WITH PunchesOrdered AS
    (
        SELECT
            ap.PunchTime,
            ap.PunchType,
            ROW_NUMBER() OVER (ORDER BY ap.PunchTime) AS rn
        FROM HR.tbl_AttendancePunches ap
        WHERE ap.EmployeeID = @EmployeeID
          AND ap.PunchTime >= @WindowStart
          AND ap.PunchTime <= @WindowEnd
    )
    SELECT *
    INTO #Punches
    FROM PunchesOrdered;

    IF EXISTS (SELECT 1 FROM #Punches)
    BEGIN
        DECLARE @PunchCount INT;
        SELECT @PunchCount = COUNT(*) FROM #Punches;

        IF @HasLunch = 1
           AND @PunchCount = 3
           AND @LunchStart IS NOT NULL
           AND @LunchEnd IS NOT NULL
        BEGIN
            DECLARE @t1 DATETIME2, @t2 DATETIME2, @t3 DATETIME2;

            SELECT
                @t1 = MIN(CASE WHEN rn = 1 THEN PunchTime END),
                @t2 = MIN(CASE WHEN rn = 2 THEN PunchTime END),
                @t3 = MIN(CASE WHEN rn = 3 THEN PunchTime END)
            FROM #Punches;

            IF (@t1 < @LunchStart AND @t2 >= @LunchEnd AND @t3 > @t2)
                DELETE FROM #Punches WHERE rn = 1;
        END;
    END;

    ;WITH P AS
    (
        SELECT
            PunchTime,
            PunchType,
            rn,
            LAG(PunchTime) OVER (ORDER BY PunchTime) AS PrevTime,
            LAG(PunchType) OVER (ORDER BY PunchTime) AS PrevType
        FROM #Punches
    )
    SELECT
        PrevTime AS StartTime,
        PunchTime AS EndTime
    INTO #Segments
    FROM P
    WHERE PrevType = 'In'
      AND PunchType = 'Out'
      AND PunchTime > PrevTime;

    DECLARE
        @FirstIn DATETIME2 = NULL,
        @LastOut DATETIME2 = NULL,
        @TotalWorkedSegments FLOAT = 0,
        @InsideShift FLOAT = 0,
        @InsideWorkBands FLOAT = 0,
        @NightMinutes INT = 0,
        @InsideMinutes FLOAT = 0,
        @OutsideMinutes FLOAT = 0,
        @AbsentMinutes INT = 0,
        @MinutesLate INT = 0,
        @TardinessMin INT = 0,
        @EarlyLeaveMinutes INT = 0,
        @OvertimeWithinSchedule FLOAT = 0,
        @OvertimeMinutes INT = 0,
        @RegularMinutes INT = 0,
        @OffScheduleMin INT = 0,
        @TotalWorkedMinutes INT = 0,
        @FoodSubsidy INT = 0,
        @HolidayMinutes INT = 0,
        @FirstInInside DATETIME2 = NULL;

    IF EXISTS (SELECT 1 FROM #Segments)
    BEGIN
        SELECT
            @FirstIn = MIN(StartTime),
            @LastOut = MAX(EndTime)
        FROM #Segments;

        ;WITH SegCalc AS
        (
            SELECT
                StartTime,
                EndTime,
                CAST(DATEDIFF(MINUTE, StartTime, EndTime) AS FLOAT) AS SegmentMinutes,

                CASE
                    WHEN EndTime <= @ShiftStart OR StartTime >= @ShiftEnd THEN 0
                    ELSE CAST(DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @ShiftStart THEN StartTime ELSE @ShiftStart END,
                        CASE WHEN EndTime   < @ShiftEnd   THEN EndTime   ELSE @ShiftEnd   END
                    ) AS FLOAT)
                END AS InsideShiftMinutes,

                CASE
                    WHEN @HasLunch = 0 OR @LunchStart IS NULL THEN 0
                    WHEN EndTime <= @ShiftStart OR StartTime >= @LunchStart THEN 0
                    ELSE CAST(DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @ShiftStart THEN StartTime ELSE @ShiftStart END,
                        CASE WHEN EndTime   < @LunchStart THEN EndTime   ELSE @LunchStart END
                    ) AS FLOAT)
                END AS InsideBlock1,

                CASE
                    WHEN @HasLunch = 0 OR @LunchEnd IS NULL THEN 0
                    WHEN EndTime <= @LunchEnd OR StartTime >= @ShiftEnd THEN 0
                    ELSE CAST(DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @LunchEnd THEN StartTime ELSE @LunchEnd END,
                        CASE WHEN EndTime   < @ShiftEnd THEN EndTime   ELSE @ShiftEnd END
                    ) AS FLOAT)
                END AS InsideBlock2,

                CASE
                    WHEN @NightStartDT IS NULL OR @NightEndDT IS NULL THEN 0
                    WHEN EndTime <= @NightStartDT OR StartTime >= @NightEndDT THEN 0
                    ELSE CAST(DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @NightStartDT THEN StartTime ELSE @NightStartDT END,
                        CASE WHEN EndTime   < @NightEndDT   THEN EndTime   ELSE @NightEndDT   END
                    ) AS FLOAT)
                END AS NightMinutes
            FROM #Segments
        )
        SELECT
            @TotalWorkedSegments = ISNULL(SUM(SegmentMinutes), 0),
            @InsideShift         = ISNULL(SUM(InsideShiftMinutes), 0),
            @InsideWorkBands     = ISNULL(SUM(InsideBlock1 + InsideBlock2), 0),
            @NightMinutes        = ISNULL(CAST(SUM(NightMinutes) AS INT), 0)
        FROM SegCalc;

        IF (@HasLunch = 1 AND @LunchStart IS NOT NULL AND @LunchEnd IS NOT NULL)
            SET @InsideMinutes = @InsideWorkBands;
        ELSE
            SET @InsideMinutes = @InsideShift;

        SET @OutsideMinutes = @TotalWorkedSegments - @InsideMinutes;
        IF @OutsideMinutes < 0 SET @OutsideMinutes = 0;

        IF (@InsideMinutes < @RequiredMin)
            SET @AbsentMinutes = @RequiredMin - CAST(@InsideMinutes AS INT);
        ELSE
            SET @AbsentMinutes = 0;

        ;WITH FirstInInsideCTE AS
        (
            SELECT TOP 1
                CASE
                    WHEN s.StartTime <= @ShiftStart AND s.EndTime > @ShiftStart THEN @ShiftStart
                    ELSE s.StartTime
                END AS FirstInInside
            FROM #Segments s
            WHERE s.EndTime > @ShiftStart
            ORDER BY s.StartTime
        )
        SELECT @FirstInInside = FirstInInside
        FROM FirstInInsideCTE;

        IF (@FirstInInside IS NOT NULL)
        BEGIN
            SET @MinutesLate = DATEDIFF(MINUTE, @ShiftStart, @FirstInInside);
            IF @MinutesLate < 0 SET @MinutesLate = 0;
        END;

        SET @TardinessMin = @MinutesLate - @GraceMin;
        IF @TardinessMin < 0 SET @TardinessMin = 0;

        IF (@LastOut IS NOT NULL AND @LastOut < @ShiftEnd)
            SET @EarlyLeaveMinutes = DATEDIFF(MINUTE, @LastOut, @ShiftEnd);
        ELSE
            SET @EarlyLeaveMinutes = 0;

        IF (@InsideMinutes > @RequiredMin)
            SET @OvertimeWithinSchedule = @InsideMinutes - @RequiredMin;
        ELSE
            SET @OvertimeWithinSchedule = 0;

        IF (@OvertimeWithinSchedule < @OTMin)
            SET @OvertimeWithinSchedule = 0;

        SET @OffScheduleMin = CAST(@OutsideMinutes AS INT);
        SET @OvertimeMinutes = CAST(@OvertimeWithinSchedule + @OutsideMinutes AS INT);
        SET @RegularMinutes = CAST(@InsideMinutes - @OvertimeWithinSchedule AS INT);
        IF @RegularMinutes < 0 SET @RegularMinutes = 0;
        SET @TotalWorkedMinutes = CAST(@InsideMinutes + @OutsideMinutes AS INT);
    END
    ELSE
    BEGIN
        SET @AbsentMinutes = @RequiredMin;
    END;

    SET @HolidayMinutes =
        CASE WHEN @IsHoliday = 1 OR @IsWeekend = 1
             THEN @TotalWorkedMinutes
             ELSE 0
        END;

    IF (@ContractType = N'Código Trabajo' AND (@RegularMinutes + @TardinessMin) >= @RequiredMin)
        SET @FoodSubsidy = 1;
    ELSE
        SET @FoodSubsidy = 0;

    MERGE HR.tbl_AttendanceCalculations AS T
    USING (SELECT @EmployeeID AS EmployeeID, @WorkDate AS WorkDate) AS S
       ON T.EmployeeID = S.EmployeeID
      AND T.WorkDate   = S.WorkDate
    WHEN MATCHED THEN
        UPDATE SET
            FirstPunchIn = @FirstIn,
            LastPunchOut = @LastOut,
            TotalWorkedMinutes = @TotalWorkedMinutes,
            RegularMinutes = @RegularMinutes,
            OvertimeMinutes = @OvertimeMinutes,
            NightMinutes = @NightMinutes,
            HolidayMinutes = @HolidayMinutes,
            RequiredMinutes = @RequiredMin,
            ScheduledWorkedMin = CAST(@InsideMinutes AS INT),
            OffScheduleMin = @OffScheduleMin,
            AbsentMinutes = @AbsentMinutes,
            MinutesLate = @MinutesLate,
            TardinessMin = @TardinessMin,
            EarlyLeaveMinutes = @EarlyLeaveMinutes,

            PermissionMinutes = 0,
            VacationMinutes = 0,
            JustificationMinutes = 0,
            MedicalLeaveMinutes = 0,
            PaidLeaveMinutes = 0,
            UnpaidLeaveMinutes = 0,
            VacationDeductedMinutes = 0,
            RecoveredMinutes = 0,

            JustificationApply = 0,
            HasPermission = 0,
            HasVacation = 0,
            HasJustification = 0,
            HasMedicalLeave = 0,
            HasManualAdjustment = 0,

            FoodSubsidy = @FoodSubsidy,

            AppliedScheduleID = @ScheduleID,
            ScheduledEntryTime = @EntryTime,
            ScheduledExitTime = @ExitTime,
            ScheduledLunchStart = @LunchStartT,
            ScheduledLunchEnd = @LunchEndT,
            ScheduledHasLunchBreak = ISNULL(@HasLunch, 0),
            ScheduledMinutes = @RequiredMin,

            Status = 'Approved',
            CalculatedAt = GETDATE(),
            CalculationSource = 'System',
            UpdatedAt = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT
        (
            EmployeeID, WorkDate, FirstPunchIn, LastPunchOut,
            TotalWorkedMinutes, RegularMinutes, OvertimeMinutes,
            NightMinutes, HolidayMinutes,
            RequiredMinutes, ScheduledWorkedMin, OffScheduleMin, AbsentMinutes,
            MinutesLate, TardinessMin, EarlyLeaveMinutes,
            PermissionMinutes, VacationMinutes, JustificationMinutes,
            MedicalLeaveMinutes, PaidLeaveMinutes, UnpaidLeaveMinutes,
            VacationDeductedMinutes, RecoveredMinutes,
            JustificationApply, HasPermission, HasVacation,
            HasJustification, HasMedicalLeave, HasManualAdjustment,
            FoodSubsidy,
            AppliedScheduleID, ScheduledEntryTime, ScheduledExitTime,
            ScheduledLunchStart, ScheduledLunchEnd, ScheduledHasLunchBreak, ScheduledMinutes,
            Status, CalculatedAt, CalculationVersion, CalculationSource, CreatedAt
        )
        VALUES
        (
            @EmployeeID, @WorkDate, @FirstIn, @LastOut,
            @TotalWorkedMinutes, @RegularMinutes, @OvertimeMinutes,
            @NightMinutes, @HolidayMinutes,
            @RequiredMin, CAST(@InsideMinutes AS INT), @OffScheduleMin, @AbsentMinutes,
            @MinutesLate, @TardinessMin, @EarlyLeaveMinutes,
            0, 0, 0,
            0, 0, 0,
            0, 0,
            0, 0, 0,
            0, 0, 0,
            @FoodSubsidy,
            @ScheduleID, @EntryTime, @ExitTime,
            @LunchStartT, @LunchEndT, ISNULL(@HasLunch, 0), @RequiredMin,
            'Approved', GETDATE(), 1, 'System', GETDATE()
        );

    DROP TABLE IF EXISTS #Segments;
    DROP TABLE IF EXISTS #Punches;
END;
GO

/*--- HR.sp_ProcessAttendanceLeavesDay -----*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceLeavesDay
(
    @EmployeeID INT,
    @WorkDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceLeavesDay
      DESCRIPCIÓN:
          Aplica permisos, vacaciones y licencias médicas al consolidado
          diario de asistencia del empleado.

      RESPONSABILIDADES:
          - Calcular minutos cubiertos por vacaciones
          - Calcular minutos cubiertos por permisos
          - Identificar permisos médicos
          - Identificar minutos que descuentan vacaciones
          - Actualizar flags relacionados
    **********************************************************************/

    DECLARE
        @EntryTime TIME,
        @ExitTime TIME,
        @HasLunch BIT,
        @LunchStartT TIME,
        @LunchEndT TIME,
        @RequiredMinutes INT,
        @DayStart DATETIME2,
        @DayEnd DATETIME2,
        @ShiftStart DATETIME2,
        @ShiftEnd DATETIME2,
        @LunchStart DATETIME2 = NULL,
        @LunchEnd DATETIME2 = NULL;

    SELECT
        @EntryTime = ScheduledEntryTime,
        @ExitTime = ScheduledExitTime,
        @HasLunch = ScheduledHasLunchBreak,
        @LunchStartT = ScheduledLunchStart,
        @LunchEndT = ScheduledLunchEnd,
        @RequiredMinutes = ScheduledMinutes
    FROM HR.tbl_AttendanceCalculations
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;

    IF @EntryTime IS NULL OR @ExitTime IS NULL
        RETURN;

    SET @DayStart = CAST(@WorkDate AS DATETIME2);
    SET @DayEnd   = DATEADD(DAY, 1, @DayStart);

    SET @ShiftStart = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @EntryTime), @DayStart);

    IF (@ExitTime <= @EntryTime)
        SET @ShiftEnd = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime), DATEADD(DAY, 1, @DayStart));
    ELSE
        SET @ShiftEnd = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime), @DayStart);

    IF (@HasLunch = 1 AND @LunchStartT IS NOT NULL AND @LunchEndT IS NOT NULL)
    BEGIN
        SET @LunchStart = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchStartT), @DayStart);
        SET @LunchEnd   = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchEndT), @DayStart);

        IF (@LunchEndT <= @LunchStartT)
            SET @LunchEnd = DATEADD(DAY, 1, @LunchEnd);
    END;

    DECLARE
        @VacationMinutes INT = 0,
        @PermissionMinutes INT = 0,
        @MedicalLeaveMinutes INT = 0,
        @VacationDeductedMinutes INT = 0,
        @PaidLeaveMinutes INT = 0,
        @UnpaidLeaveMinutes INT = 0,
        @HasVacation BIT = 0,
        @HasPermission BIT = 0,
        @HasMedicalLeave BIT = 0;

    ;WITH VacationWindows AS
    (
        SELECT
            OverlapStart = CASE WHEN CAST(v.StartDate AS DATETIME2) > @ShiftStart THEN CAST(v.StartDate AS DATETIME2) ELSE @ShiftStart END,
            OverlapEnd   = CASE WHEN DATEADD(DAY, 1, CAST(v.EndDate AS DATETIME2)) < @ShiftEnd THEN DATEADD(DAY, 1, CAST(v.EndDate AS DATETIME2)) ELSE @ShiftEnd END
        FROM HR.tbl_Vacations v
        WHERE v.EmployeeID = @EmployeeID
          AND v.Status IN ('Planned', 'InProgress', 'Completed')
          AND @WorkDate BETWEEN v.StartDate AND v.EndDate
    )
    SELECT
        @VacationMinutes = ISNULL(SUM(
            CASE
                WHEN OverlapEnd <= OverlapStart THEN 0
                ELSE DATEDIFF(MINUTE, OverlapStart, OverlapEnd)
                     - CASE
                           WHEN @HasLunch = 1
                                AND @LunchStart IS NOT NULL
                                AND @LunchEnd IS NOT NULL
                                AND OverlapStart < @LunchEnd
                                AND OverlapEnd > @LunchStart
                               THEN DATEDIFF(MINUTE,
                                     CASE WHEN OverlapStart > @LunchStart THEN OverlapStart ELSE @LunchStart END,
                                     CASE WHEN OverlapEnd   < @LunchEnd   THEN OverlapEnd   ELSE @LunchEnd END)
                           ELSE 0
                       END
            END
        ), 0)
    FROM VacationWindows;

    ;WITH PermissionWindows AS
    (
        SELECT
            pt.IsMedical,
            p.ChargedToVacation,
            OverlapStart = CASE WHEN p.StartDate > @ShiftStart THEN p.StartDate ELSE @ShiftStart END,
            OverlapEnd   = CASE WHEN p.EndDate   < @ShiftEnd   THEN p.EndDate   ELSE @ShiftEnd   END
        FROM HR.tbl_Permissions p
        INNER JOIN HR.tbl_PermissionTypes pt
            ON pt.TypeID = p.PermissionTypeID
        WHERE p.EmployeeID = @EmployeeID
          AND p.Status = 'Approved'
          AND p.StartDate < @ShiftEnd
          AND p.EndDate   > @ShiftStart
    )
    SELECT
        @PermissionMinutes = ISNULL(SUM(
            CASE
                WHEN OverlapEnd <= OverlapStart THEN 0
                ELSE DATEDIFF(MINUTE, OverlapStart, OverlapEnd)
                     - CASE
                           WHEN @HasLunch = 1
                                AND @LunchStart IS NOT NULL
                                AND @LunchEnd IS NOT NULL
                                AND OverlapStart < @LunchEnd
                                AND OverlapEnd > @LunchStart
                               THEN DATEDIFF(MINUTE,
                                     CASE WHEN OverlapStart > @LunchStart THEN OverlapStart ELSE @LunchStart END,
                                     CASE WHEN OverlapEnd   < @LunchEnd   THEN OverlapEnd   ELSE @LunchEnd END)
                           ELSE 0
                       END
            END
        ), 0),
        @MedicalLeaveMinutes = ISNULL(SUM(
            CASE
                WHEN IsMedical = 1 AND OverlapEnd > OverlapStart
                    THEN DATEDIFF(MINUTE, OverlapStart, OverlapEnd)
                         - CASE
                               WHEN @HasLunch = 1
                                    AND @LunchStart IS NOT NULL
                                    AND @LunchEnd IS NOT NULL
                                    AND OverlapStart < @LunchEnd
                                    AND OverlapEnd > @LunchStart
                                   THEN DATEDIFF(MINUTE,
                                         CASE WHEN OverlapStart > @LunchStart THEN OverlapStart ELSE @LunchStart END,
                                         CASE WHEN OverlapEnd   < @LunchEnd   THEN OverlapEnd   ELSE @LunchEnd END)
                               ELSE 0
                           END
                ELSE 0
            END
        ), 0),
        @VacationDeductedMinutes = ISNULL(SUM(
            CASE
                WHEN ChargedToVacation = 1 AND OverlapEnd > OverlapStart
                    THEN DATEDIFF(MINUTE, OverlapStart, OverlapEnd)
                         - CASE
                               WHEN @HasLunch = 1
                                    AND @LunchStart IS NOT NULL
                                    AND @LunchEnd IS NOT NULL
                                    AND OverlapStart < @LunchEnd
                                    AND OverlapEnd > @LunchStart
                                   THEN DATEDIFF(MINUTE,
                                         CASE WHEN OverlapStart > @LunchStart THEN OverlapStart ELSE @LunchStart END,
                                         CASE WHEN OverlapEnd   < @LunchEnd   THEN OverlapEnd   ELSE @LunchEnd END)
                               ELSE 0
                           END
                ELSE 0
            END
        ), 0)
    FROM PermissionWindows;

    IF @VacationMinutes < 0 SET @VacationMinutes = 0;
    IF @PermissionMinutes < 0 SET @PermissionMinutes = 0;
    IF @MedicalLeaveMinutes < 0 SET @MedicalLeaveMinutes = 0;
    IF @VacationDeductedMinutes < 0 SET @VacationDeductedMinutes = 0;

    SET @HasVacation = CASE WHEN @VacationMinutes > 0 THEN 1 ELSE 0 END;
    SET @HasPermission = CASE WHEN @PermissionMinutes > 0 THEN 1 ELSE 0 END;
    SET @HasMedicalLeave = CASE WHEN @MedicalLeaveMinutes > 0 THEN 1 ELSE 0 END;

    -- Regla provisional:
    -- PaidLeaveMinutes = permisos que no descuentan vacaciones
    -- UnpaidLeaveMinutes = 0 (hasta que exista una regla explícita de permisos no remunerados)
    SET @PaidLeaveMinutes = CASE
                                WHEN @PermissionMinutes - @VacationDeductedMinutes > 0
                                    THEN @PermissionMinutes - @VacationDeductedMinutes
                                ELSE 0
                            END;

    SET @UnpaidLeaveMinutes = 0;

    UPDATE HR.tbl_AttendanceCalculations
    SET
        VacationMinutes = @VacationMinutes,
        PermissionMinutes = @PermissionMinutes,
        MedicalLeaveMinutes = @MedicalLeaveMinutes,
        VacationDeductedMinutes = @VacationDeductedMinutes,
        PaidLeaveMinutes = @PaidLeaveMinutes,
        UnpaidLeaveMinutes = @UnpaidLeaveMinutes,
        HasVacation = @HasVacation,
        HasPermission = @HasPermission,
        HasMedicalLeave = @HasMedicalLeave,
        UpdatedAt = GETDATE()
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;
END;
GO


/*------ HR.sp_ProcessAttendanceJustificationsDay----------*/

CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceJustificationsDay
(
    @EmployeeID INT,
    @WorkDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceJustificationsDay
      DESCRIPCIÓN:
          Aplica justificaciones aprobadas de marcación sobre el consolidado
          diario del empleado.

      RESPONSABILIDADES:
          - Calcular minutos justificados
          - Marcar JustificationApply y HasJustification
          - Reducir tardanza neta
          - Reducir ausencia cuando corresponda
    **********************************************************************/

    DECLARE
        @ScheduledMinutes INT,
        @MinutesLate INT,
        @TardinessMin INT,
        @AbsentMinutes INT;

    SELECT
        @ScheduledMinutes = ScheduledMinutes,
        @MinutesLate = MinutesLate,
        @TardinessMin = TardinessMin,
        @AbsentMinutes = AbsentMinutes
    FROM HR.tbl_AttendanceCalculations
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;

    IF @ScheduledMinutes IS NULL
        RETURN;

    DECLARE @JustificationMinutes INT = 0;

    SELECT
        @JustificationMinutes = ISNULL(SUM(
            CASE
                WHEN j.HoursRequested IS NOT NULL THEN CAST(ROUND(j.HoursRequested * 60.0, 0) AS INT)
                WHEN j.StartDate IS NOT NULL AND j.EndDate IS NOT NULL AND j.EndDate > j.StartDate
                    THEN DATEDIFF(MINUTE, j.StartDate, j.EndDate)
                ELSE 0
            END
        ), 0)
    FROM HR.tbl_PunchJustifications j
    WHERE j.EmployeeID = @EmployeeID
      AND j.Status IN ('APPROVED','APPLIED')
      AND (
            CAST(j.JustificationDate AS DATE) = @WorkDate
         OR CAST(j.StartDate AS DATE) = @WorkDate
         OR CAST(j.EndDate AS DATE) = @WorkDate
          );

    IF @JustificationMinutes < 0 SET @JustificationMinutes = 0;
    IF @JustificationMinutes > @ScheduledMinutes SET @JustificationMinutes = @ScheduledMinutes;

    DECLARE
        @NewTardiness INT = @TardinessMin,
        @NewAbsent INT = @AbsentMinutes,
        @Apply BIT = CASE WHEN @JustificationMinutes > 0 THEN 1 ELSE 0 END,
        @HasJustification BIT = CASE WHEN @JustificationMinutes > 0 THEN 1 ELSE 0 END;

    -- Primero cubrir tardanza neta
    IF @JustificationMinutes > 0
    BEGIN
        DECLARE @Remaining INT = @JustificationMinutes;

        IF @Remaining >= @NewTardiness
        BEGIN
            SET @Remaining = @Remaining - @NewTardiness;
            SET @NewTardiness = 0;
        END
        ELSE
        BEGIN
            SET @NewTardiness = @NewTardiness - @Remaining;
            SET @Remaining = 0;
        END;

        -- Luego cubrir ausencia
        IF @Remaining > 0
        BEGIN
            IF @Remaining >= @NewAbsent
                SET @NewAbsent = 0;
            ELSE
                SET @NewAbsent = @NewAbsent - @Remaining;
        END;
    END;

    UPDATE HR.tbl_AttendanceCalculations
    SET
        JustificationMinutes = @JustificationMinutes,
        JustificationApply = @Apply,
        HasJustification = @HasJustification,
        TardinessMin = @NewTardiness,
        AbsentMinutes = @NewAbsent,
        UpdatedAt = GETDATE()
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;
END;
GO

/*------ HR.sp_ProcessAttendanceRecoveryDay---------*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceRecoveryDay
(
    @EmployeeID INT,
    @WorkDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceRecoveryDay
      DESCRIPCIÓN:
          Aplica minutos recuperados aprobados al consolidado diario.

      RESPONSABILIDADES:
          - Consolidar minutos recuperados desde logs
          - Ajustar ausencia si la regla lo permite
    **********************************************************************/

    DECLARE
        @RecoveredMinutes INT = 0,
        @AbsentMinutes INT = 0;

    SELECT
        @RecoveredMinutes = ISNULL(SUM(trl.MinutesRecovered), 0)
    FROM HR.tbl_TimeRecoveryPlans trp
    INNER JOIN HR.tbl_TimeRecoveryLogs trl
        ON trl.RecoveryPlanID = trp.RecoveryPlanID
    WHERE trp.EmployeeID = @EmployeeID
      AND trl.ExecutedDate = @WorkDate;

    SELECT
        @AbsentMinutes = AbsentMinutes
    FROM HR.tbl_AttendanceCalculations
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;

    IF @RecoveredMinutes < 0 SET @RecoveredMinutes = 0;
    IF @AbsentMinutes IS NULL SET @AbsentMinutes = 0;

    UPDATE HR.tbl_AttendanceCalculations
    SET
        RecoveredMinutes = @RecoveredMinutes,
        AbsentMinutes = CASE
                            WHEN @RecoveredMinutes >= @AbsentMinutes THEN 0
                            ELSE @AbsentMinutes - @RecoveredMinutes
                        END,
        UpdatedAt = GETDATE()
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;
END;
GO

/*--------- HR.sp_ProcessAttendancePlanningDay-----------*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendancePlanningDay
(
    @EmployeeID INT,
    @WorkDate   DATE,
    @Debug      BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendancePlanningDay
      DESCRIPCIÓN:
          Procesa overtime y recovery planificado para el empleado/día.

      RESPONSABILIDADES:
          - Delegar la lógica de planning al procedimiento especializado actual
          - Mantener la nueva familia de nombres homogénea
    **********************************************************************/

    EXEC HR.sp_ProcessTimePlanningForEmployeeDay
         @EmployeeID = @EmployeeID,
         @WorkDate   = @WorkDate,
         @Debug      = @Debug;
END;
GO

/*-------- HR.sp_ProcessAttendanceFinalizeDay----------*/

CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceFinalizeDay
(
    @EmployeeID INT,
    @WorkDate   DATE,
    @ContractType NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceFinalizeDay
      DESCRIPCIÓN:
          Ejecuta validaciones y normalizaciones finales sobre el consolidado
          diario de asistencia.

      RESPONSABILIDADES:
          - Evitar negativos
          - Recalcular FoodSubsidy con el estado final
          - Sellar metadatos del cálculo
    **********************************************************************/

    DECLARE
        @RegularMinutes INT,
        @TardinessMin INT,
        @RequiredMinutes INT,
        @FoodSubsidy INT = 0;

    SELECT
        @RegularMinutes = RegularMinutes,
        @TardinessMin = TardinessMin,
        @RequiredMinutes = RequiredMinutes
    FROM HR.tbl_AttendanceCalculations
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;

    IF (@ContractType = N'Código Trabajo' AND ISNULL(@RegularMinutes,0) + ISNULL(@TardinessMin,0) >= ISNULL(@RequiredMinutes,0))
        SET @FoodSubsidy = 1;
    ELSE
        SET @FoodSubsidy = 0;

    UPDATE HR.tbl_AttendanceCalculations
    SET
        TotalWorkedMinutes = CASE WHEN TotalWorkedMinutes < 0 THEN 0 ELSE TotalWorkedMinutes END,
        RegularMinutes = CASE WHEN RegularMinutes < 0 THEN 0 ELSE RegularMinutes END,
        OvertimeMinutes = CASE WHEN OvertimeMinutes < 0 THEN 0 ELSE OvertimeMinutes END,
        NightMinutes = CASE WHEN NightMinutes < 0 THEN 0 ELSE NightMinutes END,
        HolidayMinutes = CASE WHEN HolidayMinutes < 0 THEN 0 ELSE HolidayMinutes END,
        RequiredMinutes = CASE WHEN RequiredMinutes < 0 THEN 0 ELSE RequiredMinutes END,
        ScheduledWorkedMin = CASE WHEN ScheduledWorkedMin < 0 THEN 0 ELSE ScheduledWorkedMin END,
        OffScheduleMin = CASE WHEN OffScheduleMin < 0 THEN 0 ELSE OffScheduleMin END,
        AbsentMinutes = CASE WHEN AbsentMinutes < 0 THEN 0 ELSE AbsentMinutes END,
        MinutesLate = CASE WHEN MinutesLate < 0 THEN 0 ELSE MinutesLate END,
        TardinessMin = CASE WHEN TardinessMin < 0 THEN 0 ELSE TardinessMin END,
        EarlyLeaveMinutes = CASE WHEN EarlyLeaveMinutes < 0 THEN 0 ELSE EarlyLeaveMinutes END,
        PermissionMinutes = CASE WHEN PermissionMinutes < 0 THEN 0 ELSE PermissionMinutes END,
        VacationMinutes = CASE WHEN VacationMinutes < 0 THEN 0 ELSE VacationMinutes END,
        JustificationMinutes = CASE WHEN JustificationMinutes < 0 THEN 0 ELSE JustificationMinutes END,
        MedicalLeaveMinutes = CASE WHEN MedicalLeaveMinutes < 0 THEN 0 ELSE MedicalLeaveMinutes END,
        PaidLeaveMinutes = CASE WHEN PaidLeaveMinutes < 0 THEN 0 ELSE PaidLeaveMinutes END,
        UnpaidLeaveMinutes = CASE WHEN UnpaidLeaveMinutes < 0 THEN 0 ELSE UnpaidLeaveMinutes END,
        VacationDeductedMinutes = CASE WHEN VacationDeductedMinutes < 0 THEN 0 ELSE VacationDeductedMinutes END,
        RecoveredMinutes = CASE WHEN RecoveredMinutes < 0 THEN 0 ELSE RecoveredMinutes END,
        ScheduledMinutes = CASE WHEN ScheduledMinutes < 0 THEN 0 ELSE ScheduledMinutes END,
        FoodSubsidy = @FoodSubsidy,
        Status = 'Approved',
        CalculatedAt = GETDATE(),
        CalculationSource = 'System',
        UpdatedAt = GETDATE()
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;
END;
GO

/*---------HR.sp_ProcessAttendanceRunDate -----------*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceRunDate
(
    @WorkDate DATE,
    @Debug    BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceRunDate

      DESCRIPCIÓN GENERAL:
          Orquesta el procesamiento completo de asistencia para todos los
          empleados activos que tengan un horario vigente en la fecha
          especificada.

      OBJETIVO:
          Ejecutar en orden todas las etapas necesarias para construir y
          consolidar el registro diario de asistencia en
          HR.tbl_AttendanceCalculations.

      FUNCIONALIDAD:
          1. Identifica los empleados activos con horario vigente en la fecha.
          2. Ejecuta el cálculo base de asistencia por empleado.
          3. Aplica novedades administrativas como permisos y vacaciones.
          4. Aplica justificaciones aprobadas que afecten atrasos o ausencias.
          5. Aplica recuperaciones de tiempo aprobadas.
          6. Procesa planificación de horas extra o recuperación.
          7. Finaliza y normaliza el registro diario antes de dejarlo aprobado.

      DETALLE DE CADA ETAPA:
          1. Base
             Ejecuta HR.sp_ProcessAttendanceBaseDay.
             Calcula la asistencia base del día usando:
               - horario asignado
               - marcaciones de entrada/salida
               - minutos trabajados
               - atraso bruto y neto
               - ausencia
               - salida anticipada
               - minutos nocturnos
               - minutos en feriado
             Además guarda el snapshot del horario aplicado.

          2. Leaves
             Ejecuta HR.sp_ProcessAttendanceLeavesDay.
             Aplica al consolidado diario las novedades de:
               - permisos
               - vacaciones
               - licencias médicas
               - minutos que descuentan vacaciones
             También actualiza las banderas relacionadas.

          3. Justifications
             Ejecuta HR.sp_ProcessAttendanceJustificationsDay.
             Aplica las justificaciones aprobadas del empleado para:
               - cubrir tardanzas
               - reducir ausencias
               - registrar minutos justificados
               - marcar que la justificación fue aplicada

          4. Recovery
             Ejecuta HR.sp_ProcessAttendanceRecoveryDay.
             Aplica los minutos recuperados aprobados del empleado en la fecha
             y ajusta la ausencia cuando corresponda.

          5. Planning
             Ejecuta HR.sp_ProcessAttendancePlanningDay.
             Procesa la planificación aprobada del empleado para:
               - horas extra planificadas
               - recuperación planificada
               - ejecución real sobre el día trabajado

          6. Finalize
             Ejecuta HR.sp_ProcessAttendanceFinalizeDay.
             Realiza validaciones y ajustes finales del registro, por ejemplo:
               - normalización de valores
               - recalculo final de subsidio
               - actualización de estado
               - sellado de metadatos del cálculo

      CONSIDERACIONES:
          - Este procedimiento no calcula directamente la lógica de negocio
            detallada, sino que coordina procedimientos especializados.
          - Si ocurre un error en cualquier empleado, se detiene la ejecución
            y se devuelve el detalle del empleado y la fecha procesada.
          - Está diseñado para ser llamado por procesos de reproceso diario o
            por el orquestador de rango.
    **********************************************************************/

    IF @WorkDate IS NULL
        THROW 50001, 'El parametro @WorkDate es obligatorio.', 1;

    /* =========================================================
       1) PARÁMETROS DEL SISTEMA: UNA SOLA VEZ POR FECHA
       ========================================================= */
    DECLARE
        @GraceMin INT = 0,
        @OTMin INT = 0,
        @NightStart TIME = NULL,
        @NightEnd TIME = NULL,
        @IsHoliday BIT = 0,
        @IsWeekend BIT = 0;

    SELECT
        @GraceMin = ISNULL(MAX(CASE WHEN name = 'TARDINESS_GRACE_MIN'
                                    THEN TRY_CAST(Pvalues AS INT) END), 0),
        @OTMin = ISNULL(MAX(CASE WHEN name = 'OT_MIN_THRESHOLD_MIN'
                                 THEN TRY_CAST(Pvalues AS INT) END), 0),
        @NightStart = MAX(CASE WHEN name = 'NIGHT_START'
                               THEN TRY_CAST(Pvalues AS TIME) END),
        @NightEnd = MAX(CASE WHEN name = 'NIGHT_END'
                             THEN TRY_CAST(Pvalues AS TIME) END)
    FROM HR.tbl_Parameters
    WHERE name IN
    (
        'TARDINESS_GRACE_MIN',
        'OT_MIN_THRESHOLD_MIN',
        'NIGHT_START',
        'NIGHT_END'
    );

    IF @NightStart IS NULL SET @NightStart = TRY_CAST('22:00:00' AS TIME);
    IF @NightEnd   IS NULL SET @NightEnd   = TRY_CAST('06:00:00' AS TIME);

    /* =========================================================
       2) CALENDARIO DEL DÍA: UNA SOLA VEZ POR FECHA
       ========================================================= */
    SELECT
        @IsHoliday = ISNULL(IsHoliday, 0),
        @IsWeekend = ISNULL(IsWeekend, 0)
    FROM HR.vw_Calendar
    WHERE D = @WorkDate;

    /* =========================================================
       3) EMPLEADOS A PROCESAR + CONTRACT TYPE + HORARIO VIGENTE
       ========================================================= */
    DROP TABLE IF EXISTS #EmployeesToProcess;

    ;WITH CurrentSchedule AS
    (
        SELECT
            es.EmployeeID,
            es.ScheduleID,
            s.EntryTime,
            s.ExitTime,
            s.HasLunchBreak,
            s.LunchStart,
            s.LunchEnd,
            ROW_NUMBER() OVER (
                PARTITION BY es.EmployeeID
                ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC
            ) AS rn
        FROM HR.tbl_EmployeeSchedules es
        INNER JOIN HR.tbl_Schedules s
            ON s.ScheduleID = es.ScheduleID
        WHERE es.ValidFrom <= @WorkDate
          AND (es.ValidTo IS NULL OR es.ValidTo >= @WorkDate)
    )
    SELECT
        e.EmployeeID,
        ved.ContractType,
        cs.ScheduleID,
        cs.EntryTime,
        cs.ExitTime,
        cs.HasLunchBreak,
        cs.LunchStart,
        cs.LunchEnd,
        ROW_NUMBER() OVER (ORDER BY e.EmployeeID) AS RowNum
    INTO #EmployeesToProcess
    FROM HR.tbl_Employees e
    INNER JOIN CurrentSchedule cs
        ON cs.EmployeeID = e.EmployeeID
       AND cs.rn = 1
    LEFT JOIN HR.vw_EmployeeDetails ved
        ON ved.EmployeeID = e.EmployeeID
    WHERE e.IsActive = 1;

    DECLARE
        @MaxRow INT,
        @RowNum INT = 1,
        @EmployeeID INT,
        @ContractType NVARCHAR(100),
        @ScheduleID INT,
        @EntryTime TIME,
        @ExitTime TIME,
        @HasLunch BIT,
        @LunchStartT TIME,
        @LunchEndT TIME;

    SELECT @MaxRow = MAX(RowNum)
    FROM #EmployeesToProcess;

    IF @MaxRow IS NULL
        SET @MaxRow = 0;

    WHILE @RowNum <= @MaxRow
    BEGIN
        SELECT
            @EmployeeID = EmployeeID,
            @ContractType = ContractType,
            @ScheduleID = ScheduleID,
            @EntryTime = EntryTime,
            @ExitTime = ExitTime,
            @HasLunch = HasLunchBreak,
            @LunchStartT = LunchStart,
            @LunchEndT = LunchEnd
        FROM #EmployeesToProcess
        WHERE RowNum = @RowNum;

        BEGIN TRY
            EXEC HR.sp_ProcessAttendanceBaseDay
                 @EmployeeID   = @EmployeeID,
                 @WorkDate     = @WorkDate,
                 @GraceMin     = @GraceMin,
                 @OTMin        = @OTMin,
                 @NightStart   = @NightStart,
                 @NightEnd     = @NightEnd,
                 @ContractType = @ContractType,
                 @IsHoliday    = @IsHoliday,
                 @IsWeekend    = @IsWeekend,
                 @ScheduleID   = @ScheduleID,
                 @EntryTime    = @EntryTime,
                 @ExitTime     = @ExitTime,
                 @HasLunch     = @HasLunch,
                 @LunchStartT  = @LunchStartT,
                 @LunchEndT    = @LunchEndT;

            EXEC HR.sp_ProcessAttendanceLeavesDay
                 @EmployeeID = @EmployeeID,
                 @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendanceJustificationsDay
                 @EmployeeID = @EmployeeID,
                 @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendanceRecoveryDay
                 @EmployeeID = @EmployeeID,
                 @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendancePlanningDay
                 @EmployeeID = @EmployeeID,
                 @WorkDate   = @WorkDate,
                 @Debug      = @Debug;

            EXEC HR.sp_ProcessAttendanceFinalizeDay
                 @EmployeeID   = @EmployeeID,
                 @WorkDate     = @WorkDate,
                 @ContractType = @ContractType;
        END TRY
        BEGIN CATCH
            DECLARE @ErrMsg NVARCHAR(4000);
            DECLARE @ThrowMsg NVARCHAR(4000);

            SET @ErrMsg = ERROR_MESSAGE();

            SET @ThrowMsg =
                'Error procesando EmployeeID='
                + CAST(ISNULL(@EmployeeID, 0) AS VARCHAR(20))
                + ' Fecha='
                + CONVERT(VARCHAR(10), @WorkDate, 120)
                + ' -> '
                + ISNULL(@ErrMsg, 'Error no determinado.');

            THROW 50010, @ThrowMsg, 1;
        END CATCH;

        SET @RowNum += 1;
    END;

    DROP TABLE IF EXISTS #EmployeesToProcess;
END;
GO

/*---------HR.sp_ProcessAttendanceRunRange---------*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceRunRange
(
    @FromDate DATE,
    @ToDate   DATE,
    @Debug    BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceRunRange
      DESCRIPCIÓN:
          Orquesta el procesamiento completo de asistencia para un rango
          de fechas, ejecutando el flujo diario por cada fecha.

      RESPONSABILIDADES:
          - Validar rango
          - Ejecutar RunDate por cada fecha del período
          - Servir como punto de entrada para jobs y reprocesos masivos
    **********************************************************************/

    IF @FromDate IS NULL OR @ToDate IS NULL
        THROW 50020, 'Los parámetros @FromDate y @ToDate son obligatorios.', 1;

    IF @FromDate > @ToDate
        THROW 50021, 'El rango de fechas es inválido: @FromDate no puede ser mayor que @ToDate.', 1;

    DECLARE @d DATE = @FromDate;

    WHILE @d <= @ToDate
    BEGIN
        EXEC HR.sp_ProcessAttendanceRunDate
             @WorkDate = @d,
             @Debug    = @Debug;

        SET @d = DATEADD(DAY, 1, @d);
    END;
END;
GO