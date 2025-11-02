/* 📋 RESUMEN DE TRIGGERS CREADOS:
1. 🔄 trg_Contracts_SalaryHistory
Tabla: tbl_Contracts

Tipo: AFTER UPDATE

Propósito: Registrar automáticamente cambios de salario en el historial

Validaciones: Solo actúa cuando cambia el campo BaseSalary

2. ⏰ trg_Punch_Validations
Tabla: tbl_AttendancePunches

Tipo: INSTEAD OF INSERT

Validaciones:

No picadas durante vacaciones

Mínimo 5 minutos entre picadas

Empleado debe estar activo

Tipo de picada válido ('In'/'Out')

Límite de 2 picadas por tipo por día

3. 🔄 trg_Subrogations_NoOverlap
Tabla: tbl_Subrogations

Tipo: INSTEAD OF INSERT

Validaciones:

No solapamiento de períodos

Empleado subrogante disponible

Fechas coherentes

Empleados activos

No auto-subrogación

4. 📅 trg_UpdateTimestamp
Tabla: tbl_Departments

Tipo: AFTER UPDATE

Propósito: Actualizar automáticamente UpdatedAt

5. 👥 trg_Employees_UpdateTimestamp
Tabla: tbl_Employees

Tipo: AFTER UPDATE

Propósito: Actualizar automáticamente UpdatedAt

6. 👤 trg_People_UpdateTimestamp
Tabla: tbl_People

Tipo: AFTER UPDATE

Propósito: Actualizar automáticamente UpdatedAt

7. 📝 trg_Contracts_Validations
Tabla: tbl_Contracts

Tipo: INSTEAD OF INSERT, UPDATE

Validaciones:

Fechas coherentes

Empleado activo

No contratos solapados

Salario positivo

8. 🔍 trg_Audit_CriticalChanges
Tabla: tbl_Employees

Tipo: AFTER UPDATE

Propósito: Auditoría de cambios críticos

Registra: Cambios de estado y jefe inmediato

9. 🏖️ trg_Vacations_Validations
Tabla: tbl_Vacations

Tipo: INSTEAD OF INSERT, UPDATE

Validaciones:

Fechas coherentes

Empleado activo

No vacaciones solapadas

Días tomados <= días otorgados

10. 🔄 trg_Vacations_AutoStatus
Tabla: tbl_Vacations

Tipo: AFTER INSERT, UPDATE

Propósito: Actualizar estado automáticamente

Planned → InProgress (cuando llega la fecha)

InProgress → Completed (cuando termina) */
-- =============================================
-- BLOQUE 5.2: TRIGGERS COMPLETOS
-- =============================================
SET NOCOUNT ON;
PRINT 'INICIANDO CREACIÓN DE TRIGGERS...';

-- 1. TRIGGER PARA HISTORIAL DE CAMBIOS DE SALARIO
PRINT '1. Creando HR.trg_Contracts_SalaryHistory...';
IF OBJECT_ID('HR.trg_Contracts_SalaryHistory','TR') IS NOT NULL DROP TRIGGER HR.trg_Contracts_SalaryHistory;
GO
CREATE TRIGGER HR.trg_Contracts_SalaryHistory
ON HR.tbl_Contracts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Solo registrar cambios si el salario fue modificado
    IF UPDATE(BaseSalary)
    BEGIN
        INSERT INTO HR.tbl_SalaryHistory(
            ContractID, 
            OldSalary, 
            NewSalary, 
            ChangedBy, 
            ChangedAt, 
            Reason
        )
        SELECT 
            i.ContractID, 
            d.BaseSalary AS OldSalary, 
            i.BaseSalary AS NewSalary, 
            SUSER_SNAME() AS ChangedBy,
            GETDATE() AS ChangedAt,
            'Actualización de salario - Contrato: ' + i.DocumentNum + 
            CASE 
                WHEN i.Motivation IS NOT NULL THEN ' - Motivo: ' + i.Motivation
                ELSE ''
            END AS Reason
        FROM inserted i
        INNER JOIN deleted d ON i.ContractID = d.ContractID
        WHERE i.BaseSalary <> d.BaseSalary
           OR (i.BaseSalary IS NULL AND d.BaseSalary IS NOT NULL)
           OR (i.BaseSalary IS NOT NULL AND d.BaseSalary IS NULL);
    END
END
GO

-- 2. TRIGGER PARA VALIDACIONES DE PICADAS/MARCACIONES
PRINT '2. Creando HR.trg_Punch_Validations...';
IF OBJECT_ID('HR.trg_Punch_Validations','TR') IS NOT NULL DROP TRIGGER HR.trg_Punch_Validations;
GO
CREATE TRIGGER HR.trg_Punch_Validations
ON HR.tbl_AttendancePunches
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Tabla temporal para almacenar los inserted con IDs generados
    DECLARE @OutputTable TABLE (PunchId INT);

    -- 1. VALIDACIÓN: Empleado no puede picar durante vacaciones
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN HR.tbl_Vacations v ON v.EmployeeID = i.EmployeeID
        WHERE v.Status IN ('InProgress')
          AND CAST(i.PunchTime AS DATE) BETWEEN v.StartDate AND v.EndDate
    )
    BEGIN
        RAISERROR('ERROR: El empleado está de vacaciones - no se permiten marcaciones.', 16, 1);
        RETURN;
    END

    -- 2. VALIDACIÓN: Diferencia mínima de 5 minutos entre picadas
    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE EXISTS (
            SELECT 1
            FROM HR.tbl_AttendancePunches p
            WHERE p.EmployeeID = i.EmployeeID
              AND p.PunchTime < i.PunchTime
              AND DATEDIFF(MINUTE, p.PunchTime, i.PunchTime) < 5
              AND p.PunchID NOT IN (SELECT PunchID FROM inserted)  -- Clave: excluir los nuevos
        )
    )
    BEGIN
        RAISERROR('ERROR: La diferencia entre marcaciones debe ser al menos de 5 minutos.', 16, 1);
        RETURN;
    END

    -- 3. VALIDACIÓN: Empleado debe estar activo
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN HR.tbl_Employees e ON e.EmployeeID = i.EmployeeID
        WHERE e.IsActive = 0
    )
    BEGIN
        RAISERROR('ERROR: No se permiten marcaciones para empleados inactivos.', 16, 1);
        RETURN;
    END

    -- 4. VALIDACIÓN: Tipo de picada debe ser 'In' o 'Out'
    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE i.PunchType NOT IN ('In', 'Out')
    )
    BEGIN
        RAISERROR('ERROR: El tipo de marcación debe ser "In" (Entrada) o "Out" (Salida).', 16, 1);
        RETURN;
    END

    -- 5. VALIDACIÓN: No puede haber más de 2 picadas por día del mismo tipo sin justificación
    IF EXISTS (
        SELECT i.EmployeeID, CAST(i.PunchTime AS DATE) AS PunchDate, i.PunchType
        FROM inserted i
        INNER JOIN (
            SELECT EmployeeID, CAST(PunchTime AS DATE) AS PunchDate, PunchType
            FROM HR.tbl_AttendancePunches
            UNION ALL
            SELECT EmployeeID, CAST(PunchTime AS DATE) AS PunchDate, PunchType
            FROM inserted
        ) all_punches ON all_punches.EmployeeID = i.EmployeeID 
                      AND all_punches.PunchDate = CAST(i.PunchTime AS DATE)
                      AND all_punches.PunchType = i.PunchType
        GROUP BY i.EmployeeID, CAST(i.PunchTime AS DATE), i.PunchType
        HAVING COUNT(*) > 2
    )
    BEGIN
        RAISERROR('ADVERTENCIA: Se detectaron múltiples marcaciones del mismo tipo en un día. Verifique posibles errores.', 10, 1);
    END

    -- INSERTAR DATOS VÁLIDOS
    INSERT INTO HR.tbl_AttendancePunches(
        EmployeeID, 
        PunchTime, 
        PunchType, 
        DeviceID, 
        Longitude, 
        Latitude, 
        CreatedAt
    )
    OUTPUT INSERTED.PunchId INTO @OutputTable
    SELECT 
        EmployeeID, 
        PunchTime, 
        PunchType, 
        DeviceID, 
        Longitude, 
        Latitude,
        ISNULL(CreatedAt, GETDATE())
    FROM inserted;

    -- Devolver los IDs generados
    SELECT PunchId FROM @OutputTable;
END
GO

-- 3. TRIGGER PARA EVITAR SOLAPAMIENTO DE SUBROGACIONES
PRINT '3. Creando HR.trg_Subrogations_NoOverlap...';
IF OBJECT_ID('HR.trg_Subrogations_NoOverlap','TR') IS NOT NULL DROP TRIGGER HR.trg_Subrogations_NoOverlap;
GO
CREATE TRIGGER HR.trg_Subrogations_NoOverlap
ON HR.tbl_Subrogations
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validar solapamiento para el empleado subrogado
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN HR.tbl_Subrogations s ON s.SubrogatedEmployeeID = i.SubrogatedEmployeeID
        WHERE (i.StartDate <= s.EndDate AND i.EndDate >= s.StartDate)
          AND (i.SubrogationID IS NULL OR s.SubrogationID <> i.SubrogationID) -- Permite update del mismo registro
    )
    BEGIN
        RAISERROR('ERROR: El empleado ya tiene una subrogación activa en el período especificado.', 16, 1);
        RETURN;
    END

    -- Validar que el empleado subrogante no esté subrogado en el mismo período
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN HR.tbl_Subrogations s ON s.SubrogatingEmployeeID = i.SubrogatingEmployeeID
        WHERE (i.StartDate <= s.EndDate AND i.EndDate >= s.StartDate)
          AND (i.SubrogationID IS NULL OR s.SubrogationID <> i.SubrogationID)
    )
    BEGIN
        RAISERROR('ERROR: El empleado subrogante ya está actuando como subrogante en otro período.', 16, 1);
        RETURN;
    END

    -- Validar que las fechas sean coherentes
    IF EXISTS (
        SELECT 1
        FROM inserted
        WHERE StartDate > EndDate
    )
    BEGIN
        RAISERROR('ERROR: La fecha de inicio no puede ser mayor que la fecha de fin.', 16, 1);
        RETURN;
    END

    -- Validar que los empleados existan y estén activos
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN HR.tbl_Employees e1 ON e1.EmployeeID = i.SubrogatedEmployeeID
        LEFT JOIN HR.tbl_Employees e2 ON e2.EmployeeID = i.SubrogatingEmployeeID
        WHERE e1.IsActive = 0 OR e2.IsActive = 0 OR e1.EmployeeID IS NULL OR e2.EmployeeID IS NULL
    )
    BEGIN
        RAISERROR('ERROR: Uno o ambos empleados no existen o están inactivos.', 16, 1);
        RETURN;
    END

    -- Validar que no sea auto-subrogación
    IF EXISTS (
        SELECT 1
        FROM inserted
        WHERE SubrogatedEmployeeID = SubrogatingEmployeeID
    )
    BEGIN
        RAISERROR('ERROR: Un empleado no puede subrogarse a sí mismo.', 16, 1);
        RETURN;
    END

    -- INSERTAR DATOS VÁLIDOS
    INSERT INTO HR.tbl_Subrogations(
        SubrogatedEmployeeID, 
        SubrogatingEmployeeID, 
        StartDate, 
        EndDate, 
        PermissionID, 
        VacationID, 
        Reason
    )
    SELECT 
        SubrogatedEmployeeID, 
        SubrogatingEmployeeID, 
        StartDate, 
        EndDate, 
        PermissionID, 
        VacationID, 
        Reason
    FROM inserted;
END
GO

-- 4. TRIGGER PARA ACTUALIZAR TIMESTAMP AUTOMÁTICAMENTE
PRINT '4. Creando HR.trg_UpdateTimestamp...';
IF OBJECT_ID('HR.trg_UpdateTimestamp','TR') IS NOT NULL DROP TRIGGER HR.trg_UpdateTimestamp;
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

-- 5. TRIGGER PARA ACTUALIZAR TIMESTAMP EN EMPLEADOS
PRINT '5. Creando HR.trg_Employees_UpdateTimestamp...';
IF OBJECT_ID('HR.trg_Employees_UpdateTimestamp','TR') IS NOT NULL DROP TRIGGER HR.trg_Employees_UpdateTimestamp;
GO
CREATE TRIGGER HR.trg_Employees_UpdateTimestamp
ON HR.tbl_Employees
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE HR.tbl_Employees
    SET UpdatedAt = GETDATE()
    FROM HR.tbl_Employees e
    INNER JOIN inserted i ON e.EmployeeID = i.EmployeeID;
END
GO

-- 6. TRIGGER PARA ACTUALIZAR TIMESTAMP EN PERSONAS
PRINT '6. Creando HR.trg_People_UpdateTimestamp...';
IF OBJECT_ID('HR.trg_People_UpdateTimestamp','TR') IS NOT NULL DROP TRIGGER HR.trg_People_UpdateTimestamp;
GO
CREATE TRIGGER HR.trg_People_UpdateTimestamp
ON HR.tbl_People
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE HR.tbl_People
    SET UpdatedAt = GETDATE()
    FROM HR.tbl_People p
    INNER JOIN inserted i ON p.PersonID = i.PersonID;
END
GO

-- 7. TRIGGER PARA VALIDAR CONTRATOS
PRINT '7. Creando HR.trg_Contracts_Validations...';
IF OBJECT_ID('HR.trg_Contracts_Validations','TR') IS NOT NULL DROP TRIGGER HR.trg_Contracts_Validations;
GO
CREATE TRIGGER HR.trg_Contracts_Validations
ON HR.tbl_Contracts
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Validar fechas coherentes
    IF EXISTS (
        SELECT 1
        FROM inserted
        WHERE EndDate IS NOT NULL AND StartDate > EndDate
    )
    BEGIN
        RAISERROR('ERROR: La fecha de inicio no puede ser mayor que la fecha de fin.', 16, 1);
        RETURN;
    END

    -- Validar que el empleado exista y esté activo
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN HR.tbl_Employees e ON e.EmployeeID = i.EmployeeID
        WHERE e.EmployeeID IS NULL OR e.IsActive = 0
    )
    BEGIN
        RAISERROR('ERROR: El empleado especificado no existe o está inactivo.', 16, 1);
        RETURN;
    END

    -- Validar que no haya contratos solapados para el mismo empleado
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN HR.tbl_Contracts c ON c.EmployeeID = i.EmployeeID
        WHERE (i.StartDate <= ISNULL(c.EndDate, '9999-12-31') AND ISNULL(i.EndDate, '9999-12-31') >= c.StartDate)
          AND (i.ContractID IS NULL OR c.ContractID <> i.ContractID) -- Permite update del mismo contrato
    )
    BEGIN
        RAISERROR('ERROR: El empleado ya tiene un contrato activo en el período especificado.', 16, 1);
        RETURN;
    END

    -- Validar salario positivo
    IF EXISTS (
        SELECT 1
        FROM inserted
        WHERE BaseSalary <= 0
    )
    BEGIN
        RAISERROR('ERROR: El salario base debe ser mayor que cero.', 16, 1);
        RETURN;
    END

    -- Para INSERT: Insertar datos válidos
    IF NOT EXISTS (SELECT 1 FROM deleted) -- Es un INSERT
    BEGIN
        INSERT INTO HR.tbl_Contracts(
            EmployeeID, ContractType, JobID, StartDate, EndDate,
            DocumentNum, Motivation, BudgetItem, Grade, GovernanceLevel,
            Workplace, BaseSalary, CreatedBy, CreatedAt
        )
        SELECT 
            EmployeeID, ContractType, JobID, StartDate, EndDate,
            DocumentNum, Motivation, BudgetItem, Grade, GovernanceLevel,
            Workplace, BaseSalary, CreatedBy, ISNULL(CreatedAt, GETDATE())
        FROM inserted;
    END
    ELSE -- Es un UPDATE
    BEGIN
        UPDATE c
        SET 
            EmployeeID = i.EmployeeID,
            ContractType = i.ContractType,
            JobID = i.JobID,
            StartDate = i.StartDate,
            EndDate = i.EndDate,
            DocumentNum = i.DocumentNum,
            Motivation = i.Motivation,
            BudgetItem = i.BudgetItem,
            Grade = i.Grade,
            GovernanceLevel = i.GovernanceLevel,
            Workplace = i.Workplace,
            BaseSalary = i.BaseSalary,
            UpdatedBy = i.UpdatedBy,
            UpdatedAt = GETDATE()
        FROM HR.tbl_Contracts c
        INNER JOIN inserted i ON c.ContractID = i.ContractID;
    END
END
GO

-- 8. TRIGGER PARA AUDITORÍA DE CAMBIOS CRÍTICOS
PRINT '8. Creando HR.trg_Audit_CriticalChanges...';
IF OBJECT_ID('HR.trg_Audit_CriticalChanges','TR') IS NOT NULL DROP TRIGGER HR.trg_Audit_CriticalChanges;
GO
CREATE TRIGGER HR.trg_Audit_CriticalChanges
ON HR.tbl_Employees
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Registrar cambios en estado activo/inactivo
    IF UPDATE(IsActive)
    BEGIN
        INSERT INTO HR.tbl_Audit(
            TableName, 
            Action, 
            RecordID, 
            UserName, 
            DateTime, 
            Details
        )
        SELECT 
            'tbl_Employees',
            'UPDATE',
            CAST(i.EmployeeID AS NVARCHAR(64)),
            SUSER_SNAME(),
            GETDATE(),
            'Cambio de estado: ' + 
            CASE 
                WHEN i.IsActive = 1 AND d.IsActive = 0 THEN 'REACTIVACIÓN'
                WHEN i.IsActive = 0 AND d.IsActive = 1 THEN 'INACTIVACIÓN'
                ELSE 'CAMBIO DESCONOCIDO'
            END + 
            ' - Empleado: ' + CAST(i.EmployeeID AS NVARCHAR(10)) +
            ' - Usuario: ' + SUSER_SNAME()
        FROM inserted i
        INNER JOIN deleted d ON i.EmployeeID = d.EmployeeID
        WHERE i.IsActive <> d.IsActive;
    END

    -- Registrar cambios en el jefe inmediato
    IF UPDATE(ImmediateBossID)
    BEGIN
        INSERT INTO HR.tbl_Audit(
            TableName, 
            Action, 
            RecordID, 
            UserName, 
            DateTime, 
            Details
        )
        SELECT 
            'tbl_Employees',
            'UPDATE',
            CAST(i.EmployeeID AS NVARCHAR(64)),
            SUSER_SNAME(),
            GETDATE(),
            'Cambio de jefe inmediato: ' + 
            'De: ' + ISNULL(CAST(d.ImmediateBossID AS NVARCHAR(10)), 'Ninguno') +
            ' A: ' + ISNULL(CAST(i.ImmediateBossID AS NVARCHAR(10)), 'Ninguno') +
            ' - Empleado: ' + CAST(i.EmployeeID AS NVARCHAR(10))
        FROM inserted i
        INNER JOIN deleted d ON i.EmployeeID = d.EmployeeID
        WHERE ISNULL(i.ImmediateBossID, 0) <> ISNULL(d.ImmediateBossID, 0);
    END
END
GO

-- 9. TRIGGER PARA VALIDAR VACACIONES
PRINT '9. Creando HR.trg_Vacations_Validations...';
IF OBJECT_ID('HR.trg_Vacations_Validations','TR') IS NOT NULL DROP TRIGGER HR.trg_Vacations_Validations;
GO
CREATE TRIGGER HR.trg_Vacations_Validations
ON HR.tbl_Vacations
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Validar fechas coherentes
    IF EXISTS (
        SELECT 1
        FROM inserted
        WHERE StartDate > EndDate
    )
    BEGIN
        RAISERROR('ERROR: La fecha de inicio no puede ser mayor que la fecha de fin.', 16, 1);
        RETURN;
    END

    -- Validar que el empleado exista y esté activo
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN HR.tbl_Employees e ON e.EmployeeID = i.EmployeeID
        WHERE e.EmployeeID IS NULL OR e.IsActive = 0
    )
    BEGIN
        RAISERROR('ERROR: El empleado especificado no existe o está inactivo.', 16, 1);
        RETURN;
    END

    -- Validar que no haya vacaciones solapadas
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN HR.tbl_Vacations v ON v.EmployeeID = i.EmployeeID
        WHERE (i.StartDate <= v.EndDate AND i.EndDate >= v.StartDate)
          AND v.Status IN ('Planned', 'InProgress')
          AND (i.VacationID IS NULL OR v.VacationID <> i.VacationID)
    )
    BEGIN
        RAISERROR('ERROR: El empleado ya tiene vacaciones planificadas o en progreso en el período especificado.', 16, 1);
        RETURN;
    END

    -- Validar días otorgados vs días tomados
    IF EXISTS (
        SELECT 1
        FROM inserted
        WHERE DaysTaken > DaysGranted
    )
    BEGIN
        RAISERROR('ERROR: Los días tomados no pueden ser mayores que los días otorgados.', 16, 1);
        RETURN;
    END

    -- Para INSERT: Insertar datos válidos
    IF NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO HR.tbl_Vacations(
            EmployeeID, StartDate, EndDate, DaysGranted, DaysTaken, Status, CreatedAt
        )
        SELECT 
            EmployeeID, StartDate, EndDate, DaysGranted, 
            ISNULL(DaysTaken, 0), 
            ISNULL(Status, 'Planned'),
            ISNULL(CreatedAt, GETDATE())
        FROM inserted;
    END
    ELSE -- Es un UPDATE
    BEGIN
        UPDATE v
        SET 
            EmployeeID = i.EmployeeID,
            StartDate = i.StartDate,
            EndDate = i.EndDate,
            DaysGranted = i.DaysGranted,
            DaysTaken = i.DaysTaken,
            Status = i.Status,
            UpdatedAt = GETDATE()
        FROM HR.tbl_Vacations v
        INNER JOIN inserted i ON v.VacationID = i.VacationID;
    END
END
GO

-- 10. TRIGGER PARA ACTUALIZAR ESTADO DE VACACIONES AUTOMÁTICAMENTE
PRINT '10. Creando HR.trg_Vacations_AutoStatus...';
IF OBJECT_ID('HR.trg_Vacations_AutoStatus','TR') IS NOT NULL DROP TRIGGER HR.trg_Vacations_AutoStatus;
GO
CREATE TRIGGER HR.trg_Vacations_AutoStatus
ON HR.tbl_Vacations
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    -- Actualizar vacaciones que deben cambiar a "InProgress"
    UPDATE HR.tbl_Vacations
    SET Status = 'InProgress',
        UpdatedAt = GETDATE()
    WHERE Status = 'Planned'
      AND StartDate <= @Today
      AND EndDate >= @Today
      AND VacationID IN (SELECT VacationID FROM inserted);

    -- Actualizar vacaciones que deben cambiar a "Completed"
    UPDATE HR.tbl_Vacations
    SET Status = 'Completed',
        DaysTaken = DaysGranted, -- Marcar todos los días como tomados
        UpdatedAt = GETDATE()
    WHERE Status IN ('Planned', 'InProgress')
      AND EndDate < @Today
      AND VacationID IN (SELECT VacationID FROM inserted);
END
GO

PRINT 'TODOS LOS TRIGGERS CREADOS EXITOSAMENTE.';
GO