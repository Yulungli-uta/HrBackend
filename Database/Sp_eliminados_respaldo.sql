CREATE PROCEDURE HR.sp_CalculateDailyAttendance
    @EmployeeID INT,
    @WorkDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        ;WITH Punches AS (
            SELECT
                PunchTime,
                PunchType,
                ROW_NUMBER() OVER (ORDER BY PunchTime) AS RN
            FROM HR.tbl_AttendancePunches
            WHERE EmployeeID = @EmployeeID
              AND CAST(PunchTime AS DATE) = @WorkDate
        ),
        Paired AS (
            SELECT
                i.PunchTime AS InTime,
                o.PunchTime AS OutTime
            FROM Punches i
            LEFT JOIN Punches o ON o.RN = i.RN + 1
            WHERE i.PunchType = 'In' AND o.PunchType = 'Out'
        )
        SELECT
            @EmployeeID AS EmployeeID,
            @WorkDate AS WorkDate,
            MIN(InTime) AS FirstPunchIn,
            MAX(OutTime) AS LastPunchOut,
            SUM(DATEDIFF(MINUTE, InTime, OutTime)) AS TotalMinutes
        INTO #Agg
        FROM Paired;

        DECLARE @FirstPunchIn DATETIME, @LastPunchOut DATETIME, @TotalMinutes INT;
        SELECT 
            @FirstPunchIn = FirstPunchIn, 
            @LastPunchOut = LastPunchOut, 
            @TotalMinutes = ISNULL(TotalMinutes, 0) 
        FROM #Agg;

        -- Calcular minutos regulares y extras (jornada de 8 horas = 480 minutos)
        DECLARE @RegularMinutes INT = CASE WHEN @TotalMinutes > 480 THEN 480 ELSE @TotalMinutes END;
        DECLARE @OvertimeMinutes INT = CASE WHEN @TotalMinutes > 480 THEN @TotalMinutes - 480 ELSE 0 END;
        
        -- Calcular minutos nocturnos
        DECLARE @NightMinutes INT = 0;
        IF @FirstPunchIn IS NOT NULL AND @LastPunchOut IS NOT NULL
            SET @NightMinutes = HR.fn_CalculateNightMinutes(@FirstPunchIn, @LastPunchOut);
        
        -- Verificar si es feriado
        DECLARE @HolidayMinutes INT = 0;
        IF HR.fn_IsHoliday(@WorkDate) = 1
            SET @HolidayMinutes = @TotalMinutes;

        MERGE HR.tbl_AttendanceCalculations AS Target
        USING (SELECT @EmployeeID AS EmployeeID, @WorkDate AS WorkDate) AS Source
        ON Target.EmployeeID = Source.EmployeeID AND Target.WorkDate = Source.WorkDate
        WHEN MATCHED THEN
            UPDATE SET 
                FirstPunchIn = @FirstPunchIn,
                LastPunchOut = @LastPunchOut,
                TotalWorkedMinutes = @TotalMinutes,
                RegularMinutes = @RegularMinutes,
                OvertimeMinutes = @OvertimeMinutes,
                NightMinutes = @NightMinutes,
                HolidayMinutes = @HolidayMinutes,
                Status = 'Approved'
        WHEN NOT MATCHED THEN
            INSERT (EmployeeID, WorkDate, FirstPunchIn, LastPunchOut, TotalWorkedMinutes, 
                    RegularMinutes, OvertimeMinutes, NightMinutes, HolidayMinutes, Status)
            VALUES (@EmployeeID, @WorkDate, @FirstPunchIn, @LastPunchOut, @TotalMinutes,
                    @RegularMinutes, @OvertimeMinutes, @NightMinutes, @HolidayMinutes, 'Approved');

        COMMIT TRANSACTION;
        
        SELECT 
            1 AS Success,
            'Asistencia calculada exitosamente' AS Message,
            @TotalMinutes AS TotalMinutes,
            @OvertimeMinutes AS OvertimeMinutes,
            @NightMinutes AS NightMinutes;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        SELECT 
            0 AS Success,
            ERROR_MESSAGE() AS Message,
            NULL AS TotalMinutes,
            NULL AS OvertimeMinutes,
            NULL AS NightMinutes;
    END CATCH
END;

GO

CREATE PROCEDURE HR.sp_ProcessAttendanceForDate
(
    @WorkDate DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EmployeeID INT;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT EmployeeID
        FROM HR.tbl_Employees
        WHERE IsActive = 1;

    OPEN cur;
    FETCH NEXT FROM cur INTO @EmployeeID;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- 1) Procesa la asistencia base (genera tbl_AttendanceCalculations)
        EXEC HR.sp_ProcessAttendanceEmployeeDay 
             @EmployeeID = @EmployeeID,
             @WorkDate   = @WorkDate;

        -- 2) Aplica planificación (Overtime / Recovery) y actualiza 
        --    AttendanceCalculations, TimeBalances, TimePlanningExecution y Overtime
        EXEC HR.sp_ProcessTimePlanningForEmployeeDay
             @EmployeeID = @EmployeeID,
             @WorkDate   = @WorkDate,
             @Debug      = 0;   -- pon 1 para ver logs detallados									
        FETCH NEXT FROM cur INTO @EmployeeID;
    END;

    CLOSE cur;
    DEALLOCATE cur;
END;

go 

CREATE PROCEDURE HR.sp_GenerateMonthlyAttendanceReport
    @Period CHAR(7), -- Formato: 'YYYY-MM'
    @DepartmentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATE = DATEFROMPARTS(LEFT(@Period, 4), RIGHT(@Period, 2), 1);
    DECLARE @EndDate DATE = EOMONTH(@StartDate);

    SELECT 
        e.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        d.Name AS DepartmentName,
        ac.WorkDate,
        ac.FirstPunchIn,
        ac.LastPunchOut,
        ac.TotalWorkedMinutes,
        ac.RegularMinutes,
        ac.OvertimeMinutes,
        ac.NightMinutes,
        ac.HolidayMinutes,
        ac.Status AS CalculationStatus,
        CASE 
            WHEN ac.TotalWorkedMinutes >= 480 THEN 'Completo'
            WHEN ac.TotalWorkedMinutes >= 360 THEN 'Parcial'
            ELSE 'Incompleto'
        END AS DayStatus
    FROM HR.tbl_AttendanceCalculations ac
    INNER JOIN HR.tbl_Employees e ON ac.EmployeeID = e.EmployeeID
    INNER JOIN HR.tbl_People p ON e.EmployeeID = p.PersonID
    INNER JOIN HR.tbl_Departments d ON e.DepartmentID = d.DepartmentID
    WHERE ac.WorkDate BETWEEN @StartDate AND @EndDate
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
      AND e.IsActive = 1
    ORDER BY d.Name, p.LastName, p.FirstName, ac.WorkDate;
END;

GO

CREATE PROCEDURE HR.sp_ApprovePermission
    @PermissionID INT,
    @ApproverID INT,
    @Approve BIT, -- 1 = Aprobar, 0 = Rechazar
    @Comments NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus VARCHAR(20);
        SELECT @CurrentStatus = Status FROM HR.tbl_Permissions WHERE PermissionID = @PermissionID;

        IF @CurrentStatus IS NULL
        BEGIN
            RAISERROR('El permiso especificado no existe.', 16, 1);
            RETURN;
        END

        IF @CurrentStatus <> 'Pending'
        BEGIN
            RAISERROR('El permiso ya ha sido procesado.', 16, 1);
            RETURN;
        END

        UPDATE HR.tbl_Permissions
        SET 
            Status = CASE WHEN @Approve = 1 THEN 'Approved' ELSE 'Rejected' END,
            ApprovedBy = @ApproverID,
            Justification = ISNULL(Justification, '') + 
                CASE WHEN @Comments IS NOT NULL THEN CHAR(13) + CHAR(10) + 'Comentarios aprobador: ' + @Comments ELSE '' END
        WHERE PermissionID = @PermissionID;

        -- Si es aprobado y está asociado a vacaciones, actualizar el estado de las vacaciones
        IF @Approve = 1
        BEGIN
            UPDATE HR.tbl_Vacations
            SET Status = 'InProgress'
            WHERE VacationID = (SELECT VacationID FROM HR.tbl_Permissions WHERE PermissionID = @PermissionID)
              AND StartDate <= CAST(GETDATE() AS DATE)
              AND EndDate >= CAST(GETDATE() AS DATE);
        END

        COMMIT TRANSACTION;
        
        SELECT 
            1 AS Success,
            CASE WHEN @Approve = 1 THEN 'Permiso aprobado' ELSE 'Permiso rechazado' END AS Message,
            @PermissionID AS PermissionID;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        SELECT 
            0 AS Success,
            ERROR_MESSAGE() AS Message,
            NULL AS PermissionID;
    END CATCH
END;

go 

CREATE PROCEDURE HR.sp_RequestVacation
    @EmployeeID INT,
    @StartDate DATE,
    @EndDate DATE,
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validar que las fechas sean válidas
        IF @StartDate > @EndDate
        BEGIN
            RAISERROR('La fecha de inicio no puede ser mayor que la fecha de fin.', 16, 1);
            RETURN;
        END

        -- Calcular días solicitados
        DECLARE @RequestedDays INT = HR.fn_GetBusinessDays(@StartDate, @EndDate);
        
        -- Verificar saldo de vacaciones disponible
        DECLARE @AvailableDays INT = (
            SELECT ISNULL(SUM(DaysGranted - DaysTaken), 0)
            FROM HR.tbl_Vacations 
            WHERE EmployeeID = @EmployeeID 
              AND Status IN ('Planned', 'InProgress')
        );
        
        IF @RequestedDays > @AvailableDays
        BEGIN
            RAISERROR('Días de vacaciones insuficientes. Disponibles: %d, Solicitados: %d', 16, 1, @AvailableDays, @RequestedDays);
            RETURN;
        END

        -- Crear registro de vacaciones
        INSERT INTO HR.tbl_Vacations (EmployeeID, StartDate, EndDate, DaysGranted, Status)
        VALUES (@EmployeeID, @StartDate, @EndDate, @RequestedDays, 'Planned');

        DECLARE @VacationID INT = SCOPE_IDENTITY();

        -- Crear permiso automáticamente
        INSERT INTO HR.tbl_Permissions (
            EmployeeID, PermissionTypeID, StartDate, EndDate, 
            ChargedToVacation, ApprovedBy, Status, VacationID
        )
        SELECT 
            @EmployeeID,
            pt.TypeID,
            @StartDate,
            @EndDate,
            1, -- Cargado a vacaciones
            @ApproverID,
            CASE WHEN @ApproverID IS NULL THEN 'Pending' ELSE 'Approved' END,
            @VacationID
        FROM HR.tbl_PermissionTypes pt
        WHERE pt.Name = 'Vacaciones';

        COMMIT TRANSACTION;
        
        SELECT 
            1 AS Success,
            'Vacaciones solicitadas exitosamente' AS Message,
            @VacationID AS VacationID,
            @RequestedDays AS DaysGranted;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        SELECT 
            0 AS Success,
            ERROR_MESSAGE() AS Message,
            NULL AS VacationID,
            NULL AS DaysGranted;
    END CATCH
END;

GO 

CREATE PROCEDURE HR.sp_PlanOvertime
    @EmployeeID INT,
    @WorkDate DATE,
    @Hours DECIMAL(5,2),
    @OvertimeType VARCHAR(50),
    @ApproverID INT = NULL,
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validar que el empleado exista y esté activo
        IF NOT EXISTS (SELECT 1 FROM HR.tbl_Employees WHERE EmployeeID = @EmployeeID AND IsActive = 1)
        BEGIN
            RAISERROR('El empleado especificado no existe o no está activo.', 16, 1);
            RETURN;
        END

        -- Validar que el tipo de hora extra exista
        IF NOT EXISTS (SELECT 1 FROM HR.tbl_OvertimeConfig WHERE OvertimeType = @OvertimeType)
        BEGIN
            RAISERROR('El tipo de hora extra especificado no existe.', 16, 1);
            RETURN;
        END

        -- Obtener factor
        DECLARE @Factor DECIMAL(5,2);
        SELECT @Factor = Factor FROM HR.tbl_OvertimeConfig WHERE OvertimeType = @OvertimeType;

        -- Verificar que no exista ya una planificación para la misma fecha y empleado
        IF EXISTS (
            SELECT 1 
            FROM HR.tbl_Overtime 
            WHERE EmployeeID = @EmployeeID 
              AND WorkDate = @WorkDate 
              AND OvertimeType = @OvertimeType
              AND Status IN ('Planned', 'Verified')
        )
        BEGIN
            RAISERROR('Ya existe una planificación de horas extra para este empleado en la fecha especificada.', 16, 1);
            RETURN;
        END

        -- Insertar planificación
        INSERT INTO HR.tbl_Overtime (
            EmployeeID, WorkDate, OvertimeType, Hours, Status, 
            ApprovedBy, Factor, ActualHours, PaymentAmount
        )
        VALUES (
            @EmployeeID, @WorkDate, @OvertimeType, @Hours, 
            CASE WHEN @ApproverID IS NULL THEN 'Planned' ELSE 'Verified' END,
            @ApproverID, @Factor, 0, 0
        );

        DECLARE @OvertimeID INT = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
        
        SELECT 
            1 AS Success,
            'Horas extra planificadas exitosamente' AS Message,
            @OvertimeID AS OvertimeID;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        SELECT 
            0 AS Success,
            ERROR_MESSAGE() AS Message,
            NULL AS OvertimeID;
    END CATCH
END;

GO 

CREATE PROCEDURE HR.sp_GeneratePayrollReport
    @Period CHAR(7),
    @DepartmentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.EmployeeID,
        per.FirstName + ' ' + per.LastName AS EmployeeName,
        d.Name AS DepartmentName,
        p.Period,
        p.BaseSalary,
        p.Status AS PayrollStatus,
        p.PaymentDate,
        p.BankAccount,
        -- Totales
        (SELECT ISNULL(SUM(pl.Quantity * pl.UnitValue), 0) 
         FROM HR.tbl_PayrollLines pl 
         WHERE pl.PayrollID = p.PayrollID AND pl.LineType IN ('Earning', 'Overtime', 'Subsidy')) AS TotalIngresos,
        (SELECT ISNULL(SUM(pl.Quantity * pl.UnitValue), 0) 
         FROM HR.tbl_PayrollLines pl 
         WHERE pl.PayrollID = p.PayrollID AND pl.LineType = 'Deduction') AS TotalDeducciones,
        (SELECT ISNULL(SUM(pl.Quantity * pl.UnitValue), 0) 
         FROM HR.tbl_PayrollLines pl 
         WHERE pl.PayrollID = p.PayrollID AND pl.LineType = 'Overtime') AS TotalHorasExtra
    FROM HR.tbl_Payroll p
    INNER JOIN HR.tbl_Employees e ON p.EmployeeID = e.EmployeeID
    INNER JOIN HR.tbl_People per ON e.EmployeeID = per.PersonID
    INNER JOIN HR.tbl_Departments d ON e.DepartmentID = d.DepartmentID
    WHERE p.Period = @Period
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
      AND e.IsActive = 1
    ORDER BY d.Name, per.LastName, per.FirstName;
END;

GO 

CREATE PROCEDURE HR.sp_GenerateLeaveReport
    @StartDate DATE,
    @EndDate DATE,
    @DepartmentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Vacaciones
    SELECT 
        e.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        d.Name AS DepartmentName,
        'Vacaciones' AS LeaveType,
        v.StartDate,
        v.EndDate,
        v.DaysGranted,
        v.DaysTaken,
        v.Status,
        NULL AS PermissionStatus
    FROM HR.tbl_Vacations v
    INNER JOIN HR.tbl_Employees e ON v.EmployeeID = e.EmployeeID
    INNER JOIN HR.tbl_People p ON e.EmployeeID = p.PersonID
    INNER JOIN HR.tbl_Departments d ON e.DepartmentID = d.DepartmentID
    WHERE (v.StartDate BETWEEN @StartDate AND @EndDate OR v.EndDate BETWEEN @StartDate AND @EndDate)
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
    
    UNION ALL
    
    -- Permisos
    SELECT 
        e.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        d.Name AS DepartmentName,
        pt.Name AS LeaveType,
        pm.StartDate,
        pm.EndDate,
        DATEDIFF(DAY, pm.StartDate, pm.EndDate) + 1 AS DaysGranted,
        NULL AS DaysTaken,
        NULL AS VacationStatus,
        pm.Status AS PermissionStatus
    FROM HR.tbl_Permissions pm
    INNER JOIN HR.tbl_PermissionTypes pt ON pm.PermissionTypeID = pt.TypeID
    INNER JOIN HR.tbl_Employees e ON pm.EmployeeID = e.EmployeeID
    INNER JOIN HR.tbl_People p ON e.EmployeeID = p.PersonID
    INNER JOIN HR.tbl_Departments d ON e.DepartmentID = d.DepartmentID
    WHERE (pm.StartDate BETWEEN @StartDate AND @EndDate OR pm.EndDate BETWEEN @StartDate AND @EndDate)
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
    
    ORDER BY DepartmentName, EmployeeName, StartDate;
END;

GO 

