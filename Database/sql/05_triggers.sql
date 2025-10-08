-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE TRIGGER HR.trg_Contracts_SalaryHistory
ON HR.tbl_Contracts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO HR.tbl_SalaryHistory(ContractID, OldSalary, NewSalary, Reason)
    SELECT i.ContractID, d.BaseSalary, i.BaseSalary, 'Cambio de salario'
    FROM inserted i
    JOIN deleted d ON i.ContractID = d.ContractID
    WHERE ISNULL(i.BaseSalary,0) <> ISNULL(d.BaseSalary,0);
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE TRIGGER HR.trg_Punch_Validations
ON HR.tbl_AttendancePunches
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN HR.tbl_Vacations v
          ON v.EmployeeID = i.EmployeeID
         AND v.Status IN ('InProgress')
         AND CAST(i.PunchTime AS date) BETWEEN v.StartDate AND v.EndDate
    )
    BEGIN
        RAISERROR(N'El empleado está de vacaciones: no se permiten picadas.',16,1);
        RETURN;
    END

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
        RAISERROR(N'La diferencia entre picadas debe ser al menos de 5 minutos.',16,1);
        RETURN;
    END

    INSERT INTO HR.tbl_AttendancePunches(EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude)
    SELECT EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude
    FROM inserted;
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE TRIGGER HR.trg_Overtime_NoPermission
ON HR.tbl_Overtime
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN HR.tbl_Permissions p
          ON p.EmployeeID = i.EmployeeID
         AND i.WorkDate BETWEEN p.StartDate AND p.EndDate
         AND p.Status IN ('Pending','Approved')
    )
    BEGIN
        RAISERROR(N'No se permiten horas extras cuando existe un permiso en la fecha.',16,1);
        RETURN;
    END

    INSERT INTO HR.tbl_Overtime(EmployeeID, WorkDate, OvertimeType, Hours, Status, ApprovedBy, SecondApprover, Factor, ActualHours, PaymentAmount)
    SELECT EmployeeID, WorkDate, OvertimeType, Hours, Status, ApprovedBy, SecondApprover, Factor, ActualHours, PaymentAmount
    FROM inserted;
END
GO */

IF OBJECT_ID('HR.trg_Subrogations_NoOverlap','TR') IS NOT NULL DROP TRIGGER HR.trg_Subrogations_NoOverlap;
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE TRIGGER HR.trg_Subrogations_NoOverlap
ON HR.tbl_Subrogations
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN HR.tbl_Subrogations s
          ON s.SubrogatedEmployeeID = i.SubrogatedEmployeeID
         AND (i.StartDate <= s.EndDate AND i.EndDate >= s.StartDate)
    )
    BEGIN
        RAISERROR(N'Un empleado no puede tener dos subrogaciones simultáneas.',16,1);
        RETURN;
    END

    INSERT INTO HR.tbl_Subrogations(SubrogatedEmployeeID, SubrogatingEmployeeID, StartDate, EndDate, PermissionID, VacationID, Reason)
    SELECT SubrogatedEmployeeID, SubrogatingEmployeeID, StartDate, EndDate, PermissionID, VacationID, Reason
    FROM inserted;
END
GO