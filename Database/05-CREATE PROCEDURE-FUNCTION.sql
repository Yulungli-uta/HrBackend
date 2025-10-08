/*
📊 RESUMEN DE OBJETOS CREADOS:
🔧 FUNCIONES (5):
fn_GetBusinessDays - Días laborables entre fechas

fn_CalculateNightMinutes - Minutos en horario nocturno

fn_GetCurrentBaseSalary - Salario base actual

fn_IsHoliday - Verificar si es feriado

fn_GetActiveSchedule - Horario activo de empleado

⚡ TRIGGERS (4):
trg_Contracts_SalaryHistory - Historial de salarios

trg_Punch_Validations - Validaciones de marcaciones

trg_Subrogations_NoOverlap - Evitar solapamiento

trg_UpdateTimestamp - Actualizar fechas automáticamente

🚀 PROCEDIMIENTOS PRINCIPALES (7):
sp_CalculateDailyAttendance - Cálculo diario de asistencia

sp_CalculateMonthlyPayroll - Cálculo de nómina mensual

sp_RequestVacation - Solicitud de vacaciones

sp_ApprovePermission - Aprobación de permisos

sp_PlanOvertime - Planificación de horas extra

sp_RegisterPersonnelMovement - Movimientos de personal

sp_GenerateMonthlyAttendanceReport - Reporte de asistencia

sp_GeneratePayrollReport - Reporte de nómina

sp_GenerateLeaveReport - Reporte de vacaciones/permisos
*/

-- =============================================
-- BLOQUE 5: PROCEDIMIENTOS Y FUNCIONES COMPLETOS
-- ORDEN: 1) Funciones 2) Triggers 3) Procedimientos
-- =============================================
SET NOCOUNT ON;
PRINT 'INICIANDO CREACIÓN DE FUNCIONES, TRIGGERS Y PROCEDIMIENTOS...';

-- =============================================
-- 5.1 FUNCIONES
-- =============================================
PRINT '5.1. Creando funciones...';

-- Función para calcular días laborables entre dos fechas
IF OBJECT_ID('HR.fn_GetBusinessDays') IS NOT NULL DROP FUNCTION HR.fn_GetBusinessDays;
GO
CREATE FUNCTION HR.fn_GetBusinessDays(
    @StartDate DATE,
    @EndDate DATE
)
RETURNS INT
AS
BEGIN
    DECLARE @TotalDays INT = DATEDIFF(DAY, @StartDate, @EndDate) + 1;
    DECLARE @WeekendDays INT = 0;
    DECLARE @CurrentDate DATE = @StartDate;
    
    WHILE @CurrentDate <= @EndDate
    BEGIN
        IF DATEPART(WEEKDAY, @CurrentDate) IN (1, 7) -- 1=Domingo, 7=Sábado
            SET @WeekendDays = @WeekendDays + 1;
        SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
    END
    
    RETURN @TotalDays - @WeekendDays;
END
GO

-- Función para calcular minutos nocturnos (22:00 - 06:00)
IF OBJECT_ID('HR.fn_CalculateNightMinutes') IS NOT NULL DROP FUNCTION HR.fn_CalculateNightMinutes;
GO
CREATE FUNCTION HR.fn_CalculateNightMinutes(
    @StartTime DATETIME,
    @EndTime DATETIME
)
RETURNS INT
AS
BEGIN
    DECLARE @NightMinutes INT = 0;
    DECLARE @CurrentTime DATETIME = @StartTime;
    
    WHILE @CurrentTime < @EndTime
    BEGIN
        DECLARE @NextMinute DATETIME = DATEADD(MINUTE, 1, @CurrentTime);
        DECLARE @Hour INT = DATEPART(HOUR, @CurrentTime);
        
        -- Horario nocturno: 22:00 a 06:00
        IF @Hour >= 22 OR @Hour < 6
            SET @NightMinutes = @NightMinutes + 1;
            
        SET @CurrentTime = @NextMinute;
    END
    
    RETURN @NightMinutes;
END
GO

-- Función para obtener salario base actual de un empleado
IF OBJECT_ID('HR.fn_GetCurrentBaseSalary') IS NOT NULL DROP FUNCTION HR.fn_GetCurrentBaseSalary;
GO
CREATE FUNCTION HR.fn_GetCurrentBaseSalary(
    @EmployeeID INT
)
RETURNS DECIMAL(12,2)
AS
BEGIN
    DECLARE @Salary DECIMAL(12,2);
    
    SELECT TOP 1 @Salary = BaseSalary
    FROM HR.tbl_Contracts
    WHERE EmployeeID = @EmployeeID
      AND GETDATE() BETWEEN StartDate AND ISNULL(EndDate, '9999-12-31')
      AND IsActive = 1
    ORDER BY StartDate DESC;
    
    RETURN ISNULL(@Salary, 0);
END
GO

-- Función para verificar si una fecha es feriado
IF OBJECT_ID('HR.fn_IsHoliday') IS NOT NULL DROP FUNCTION HR.fn_IsHoliday;
GO
CREATE FUNCTION HR.fn_IsHoliday(
    @CheckDate DATE
)
RETURNS BIT
AS
BEGIN
    DECLARE @IsHoliday BIT = 0;
    
    IF EXISTS (
        SELECT 1 
        FROM HR.tbl_Holidays 
        WHERE HolidayDate = @CheckDate 
          AND IsActive = 1
    )
        SET @IsHoliday = 1;
        
    RETURN @IsHoliday;
END
GO

-- Función para obtener el horario activo de un empleado
IF OBJECT_ID('HR.fn_GetActiveSchedule') IS NOT NULL DROP FUNCTION HR.fn_GetActiveSchedule;
GO
CREATE FUNCTION HR.fn_GetActiveSchedule(
    @EmployeeID INT,
    @ForDate DATE
)
RETURNS TABLE
AS
RETURN (
    SELECT TOP 1 s.*
    FROM HR.tbl_EmployeeSchedules es
    JOIN HR.tbl_Schedules s ON s.ScheduleID = es.ScheduleID
    WHERE es.EmployeeID = @EmployeeID
      AND @ForDate BETWEEN es.ValidFrom AND ISNULL(es.ValidTo, '9999-12-31')
    ORDER BY es.ValidFrom DESC
);
GO

-- =============================================
-- 5.2 TRIGGERS
-- =============================================
PRINT '5.2. Creando triggers...';

-- Trigger para historial de cambios de salario
IF OBJECT_ID('HR.trg_Contracts_SalaryHistory') IS NOT NULL DROP TRIGGER HR.trg_Contracts_SalaryHistory;
GO
CREATE TRIGGER HR.trg_Contracts_SalaryHistory
ON HR.tbl_Contracts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO HR.tbl_SalaryHistory(ContractID, OldSalary, NewSalary, Reason)
    SELECT 
        i.ContractID, 
        d.BaseSalary, 
        i.BaseSalary, 
        'Actualización de salario - ' + ISNULL(i.Motivation, 'Sin motivo especificado')
    FROM inserted i
    INNER JOIN deleted d ON i.ContractID = d.ContractID
    WHERE i.BaseSalary <> d.BaseSalary;
END
GO

-- Trigger para validaciones de picadas
IF OBJECT_ID('HR.trg_Punch_Validations') IS NOT NULL DROP TRIGGER HR.trg_Punch_Validations;
GO
CREATE TRIGGER HR.trg_Punch_Validations
ON HR.tbl_AttendancePunches
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Crear tabla temporal para almacenar los inserted con IDs generados
    DECLARE @OutputTable TABLE (PunchId INT);

    -- Verificar vacaciones
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN HR.tbl_Vacations v ON v.EmployeeID = i.EmployeeID
        WHERE v.Status IN ('InProgress')
          AND CAST(i.PunchTime AS DATE) BETWEEN v.StartDate AND v.EndDate
    )
    BEGIN
        RAISERROR('El empleado está de vacaciones: no se permiten picadas.', 16, 1);
        RETURN;
    END

    -- Verificar diferencia de tiempo mínima (5 minutos)
    IF EXISTS (
        SELECT 1
        FROM inserted i
        CROSS APPLY (
            SELECT TOP 1 p.PunchTime
            FROM HR.tbl_AttendancePunches p
            WHERE p.EmployeeID = i.EmployeeID
            ORDER BY p.PunchTime DESC
        ) prev
        WHERE DATEDIFF(MINUTE, prev.PunchTime, i.PunchTime) < 5
    )
    BEGIN
        RAISERROR('La diferencia entre picadas debe ser al menos de 5 minutos.', 16, 1);
        RETURN;
    END

    -- Insertar válido y capturar los IDs generados
    INSERT INTO HR.tbl_AttendancePunches(EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude, CreatedAt)
    OUTPUT INSERTED.PunchId INTO @OutputTable
    SELECT EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude, CreatedAt
    FROM inserted;

    -- Devolver los IDs generados
    SELECT PunchId FROM @OutputTable;
END
GO

-- Trigger para evitar solapamiento de subrogaciones
IF OBJECT_ID('HR.trg_Subrogations_NoOverlap') IS NOT NULL DROP TRIGGER HR.trg_Subrogations_NoOverlap;
GO
CREATE TRIGGER HR.trg_Subrogations_NoOverlap
ON HR.tbl_Subrogations
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Verificar solapamiento
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN HR.tbl_Subrogations s ON s.SubrogatedEmployeeID = i.SubrogatedEmployeeID
        WHERE (i.StartDate <= s.EndDate AND i.EndDate >= s.StartDate)
          AND s.SubrogationID <> i.SubrogationID
    )
    BEGIN
        RAISERROR('Un empleado no puede tener dos subrogaciones simultáneas.', 16, 1);
        RETURN;
    END

    -- Insertar válido
    INSERT INTO HR.tbl_Subrogations(
        SubrogatedEmployeeID, SubrogatingEmployeeID, StartDate, EndDate, 
        PermissionID, VacationID, Reason
    )
    SELECT 
        SubrogatedEmployeeID, SubrogatingEmployeeID, StartDate, EndDate, 
        PermissionID, VacationID, Reason
    FROM inserted;
END
GO

-- Trigger para actualizar UpdatedAt automáticamente
IF OBJECT_ID('HR.trg_UpdateTimestamp') IS NOT NULL DROP TRIGGER HR.trg_UpdateTimestamp;
GO
CREATE TRIGGER HR.trg_UpdateTimestamp
ON HR.tbl_Departments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Departments
    SET UpdatedAt = GETDATE()
    FROM HR.tbl_Departments d
    INNER JOIN inserted i ON d.DepartmentID = i.DepartmentID;
END
GO

-- =============================================
-- 5.3 PROCEDIMIENTOS ALMACENADOS PRINCIPALES
-- =============================================
PRINT '5.3. Creando procedimientos almacenados principales...';

-- Procedimiento para cálculo diario de asistencia
IF OBJECT_ID('HR.sp_CalculateDailyAttendance') IS NOT NULL DROP PROCEDURE HR.sp_CalculateDailyAttendance;
GO
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
END
GO

-- Procedimiento para cálculo de nómina mensual
IF OBJECT_ID('HR.sp_CalculateMonthlyPayroll') IS NOT NULL DROP PROCEDURE HR.sp_CalculateMonthlyPayroll;
GO
CREATE PROCEDURE HR.sp_CalculateMonthlyPayroll
    @EmployeeID INT,
    @Period CHAR(7) -- Formato: 'YYYY-MM'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @StartDate DATE = DATEFROMPARTS(
            LEFT(@Period, 4), 
            RIGHT(@Period, 2), 
            1
        );
        DECLARE @EndDate DATE = EOMONTH(@StartDate);

        -- Obtener salario base actual
        DECLARE @BaseSalary DECIMAL(12,2) = HR.fn_GetCurrentBaseSalary(@EmployeeID);

        -- Crear o actualizar cabecera de nómina
        IF NOT EXISTS (SELECT 1 FROM HR.tbl_Payroll WHERE EmployeeID = @EmployeeID AND Period = @Period)
        BEGIN
            INSERT INTO HR.tbl_Payroll (EmployeeID, Period, BaseSalary, Status)
            VALUES (@EmployeeID, @Period, @BaseSalary, 'Pending');
        END
        ELSE
        BEGIN
            UPDATE HR.tbl_Payroll 
            SET BaseSalary = @BaseSalary 
            WHERE EmployeeID = @EmployeeID AND Period = @Period;
        END

        DECLARE @PayrollID INT = (
            SELECT PayrollID 
            FROM HR.tbl_Payroll 
            WHERE EmployeeID = @EmployeeID AND Period = @Period
        );

        -- Calcular horas extra del período
        INSERT INTO HR.tbl_PayrollLines (PayrollID, LineType, Concept, Quantity, UnitValue, Notes)
        SELECT 
            @PayrollID,
            'Overtime',
            CONCAT('Horas extra ', ot.OvertimeType, ' - ', CONVERT(VARCHAR, ot.WorkDate, 103)),
            ot.ActualHours,
            ROUND((@BaseSalary / 240) * ot.Factor, 2), -- 240 horas mensuales
            CONCAT('Factor: ', ot.Factor, ' - Estado: ', ot.Status)
        FROM HR.tbl_Overtime ot
        WHERE ot.EmployeeID = @EmployeeID
          AND ot.WorkDate BETWEEN @StartDate AND @EndDate
          AND ot.Status = 'Verified'
          AND NOT EXISTS (
            SELECT 1 
            FROM HR.tbl_PayrollLines pl 
            WHERE pl.PayrollID = @PayrollID 
              AND pl.LineType = 'Overtime'
              AND pl.Concept LIKE CONCAT('%', ot.OvertimeType, '%', CONVERT(VARCHAR, ot.WorkDate, 103), '%')
          );

        -- Agregar línea de salario base
        IF NOT EXISTS (SELECT 1 FROM HR.tbl_PayrollLines WHERE PayrollID = @PayrollID AND LineType = 'Earning')
        BEGIN
            INSERT INTO HR.tbl_PayrollLines (PayrollID, LineType, Concept, Quantity, UnitValue)
            VALUES (@PayrollID, 'Earning', 'Salario Base', 1, @BaseSalary);
        END

        COMMIT TRANSACTION;
        
        SELECT 
            1 AS Success,
            'Nómina calculada exitosamente' AS Message,
            @PayrollID AS PayrollID;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        SELECT 
            0 AS Success,
            ERROR_MESSAGE() AS Message,
            NULL AS PayrollID;
    END CATCH
END
GO

-- Procedimiento para solicitar vacaciones
IF OBJECT_ID('HR.sp_RequestVacation') IS NOT NULL DROP PROCEDURE HR.sp_RequestVacation;
GO
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
END
GO

-- Procedimiento para aprobar/rechazar permisos
IF OBJECT_ID('HR.sp_ApprovePermission') IS NOT NULL DROP PROCEDURE HR.sp_ApprovePermission;
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
END
GO

-- Procedimiento para planificar horas extra
IF OBJECT_ID('HR.sp_PlanOvertime') IS NOT NULL DROP PROCEDURE HR.sp_PlanOvertime;
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
END
GO

-- Procedimiento para registrar movimiento de personal
IF OBJECT_ID('HR.sp_RegisterPersonnelMovement') IS NOT NULL DROP PROCEDURE HR.sp_RegisterPersonnelMovement;
GO
CREATE PROCEDURE HR.sp_RegisterPersonnelMovement
    @EmployeeID INT,
    @ContractID INT,
    @DestinationDepartmentID INT,
    @MovementDate DATE,
    @MovementType VARCHAR(30),
    @Reason NVARCHAR(500) = NULL,
    @DocumentLocation NVARCHAR(255) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Obtener departamento actual
        DECLARE @CurrentDepartmentID INT;
        SELECT @CurrentDepartmentID = DepartmentID 
        FROM HR.tbl_Employees 
        WHERE EmployeeID = @EmployeeID;

        -- Validar que el destino sea diferente al origen
        IF @CurrentDepartmentID = @DestinationDepartmentID
        BEGIN
            RAISERROR('El departamento destino debe ser diferente al departamento actual.', 16, 1);
            RETURN;
        END

        -- Registrar movimiento
        INSERT INTO HR.tbl_PersonnelMovements (
            EmployeeID, ContractID, OriginDepartmentID, DestinationDepartmentID,
            MovementDate, MovementType, DocumentLocation, Reason, CreatedBy
        )
        VALUES (
            @EmployeeID, @ContractID, @CurrentDepartmentID, @DestinationDepartmentID,
            @MovementDate, @MovementType, @DocumentLocation, @Reason, @CreatedBy
        );

        -- Actualizar departamento del empleado
        UPDATE HR.tbl_Employees
        SET 
            DepartmentID = @DestinationDepartmentID,
            UpdatedBy = @CreatedBy,
            UpdatedAt = GETDATE()
        WHERE EmployeeID = @EmployeeID;

        DECLARE @MovementID INT = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
        
        SELECT 
            1 AS Success,
            'Movimiento de personal registrado exitosamente' AS Message,
            @MovementID AS MovementID;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        SELECT 
            0 AS Success,
            ERROR_MESSAGE() AS Message,
            NULL AS MovementID;
    END CATCH
END
GO

-- =============================================
-- 5.4 PROCEDIMIENTOS DE REPORTES
-- =============================================
PRINT '5.4. Creando procedimientos de reportes...';

-- Reporte de asistencia mensual
IF OBJECT_ID('HR.sp_GenerateMonthlyAttendanceReport') IS NOT NULL DROP PROCEDURE HR.sp_GenerateMonthlyAttendanceReport;
GO
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
END
GO

-- Reporte de nómina detallada
IF OBJECT_ID('HR.sp_GeneratePayrollReport') IS NOT NULL DROP PROCEDURE HR.sp_GeneratePayrollReport;
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
END
GO

-- Reporte de vacaciones y permisos
IF OBJECT_ID('HR.sp_GenerateLeaveReport') IS NOT NULL DROP PROCEDURE HR.sp_GenerateLeaveReport;
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
END
GO

PRINT 'TODAS LAS FUNCIONES, TRIGGERS Y PROCEDIMIENTOS CREADOS EXITOSAMENTE.';
GO