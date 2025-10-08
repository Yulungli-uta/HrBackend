-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE VIEW HR.vw_EmployeeDetails AS
SELECT 
    p.PersonID      AS EmployeeID,
    p.FirstName, p.LastName, p.IDCard, e.Email,
    e.Type          AS EmployeeType,
    d.Name          AS Department,
    --f.Name          AS Faculty,
    c.BaseSalary,
    e.HireDate
FROM HR.tbl_People p
JOIN HR.tbl_Employees e ON e.EmployeeID = p.PersonID
LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
--LEFT JOIN HR.tbl_Faculties   f ON f.FacultyID = d.FacultyID
OUTER APPLY (
    SELECT TOP 1 c1.BaseSalary
    FROM HR.tbl_Contracts c1
    WHERE c1.EmployeeID = e.EmployeeID
      AND GETDATE() BETWEEN c1.StartDate AND ISNULL(c1.EndDate,'9999-12-31')
    ORDER BY c1.StartDate DESC
) c;
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE VIEW HR.vw_EmployeeLeaveStatus AS
SELECT 
    e.EmployeeID,
    p.FirstName + ' ' + p.LastName AS EmployeeName,
    ISNULL(v.DaysGranted,0) AS DaysGranted,
    ISNULL(v.DaysTaken,0)   AS DaysTaken,
    ISNULL(v.DaysGranted,0) - ISNULL(v.DaysTaken,0) AS DaysRemaining,
    (SELECT COUNT(*) FROM HR.tbl_Permissions perm 
      WHERE perm.EmployeeID = e.EmployeeID AND perm.Status='Pending') AS PendingPermissions
FROM HR.tbl_Employees e
JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
LEFT JOIN HR.tbl_Vacations v ON v.EmployeeID = e.EmployeeID;
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE VIEW HR.vw_EmployeeMovementHistory AS
SELECT
    m.MovementID,
    m.MovementDate,
    e.EmployeeID,
    p.FirstName + ' ' + p.LastName AS EmployeeName,
    orig.Name AS OriginDepartment,
    dest.Name AS DestinationDepartment,
    m.MovementType,
    c.ContractType,
    c.StartDate AS ContractStart,
    c.EndDate   AS ContractEnd,
    c.BaseSalary
FROM HR.tbl_PersonnelMovements m
JOIN HR.tbl_Employees e ON m.EmployeeID = e.EmployeeID
JOIN HR.tbl_People p ON e.EmployeeID = p.PersonID
LEFT JOIN HR.tbl_Departments orig ON m.OriginDepartmentID = orig.DepartmentID
JOIN HR.tbl_Departments dest ON m.DestinationDepartmentID = dest.DepartmentID
JOIN HR.tbl_Contracts c ON m.ContractID = c.ContractID;
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE VIEW HR.vw_PendingOvertimeApproval AS
SELECT
    o.OvertimeID,
    e.EmployeeID,
    p.FirstName + ' ' + p.LastName AS EmployeeName,
    d.Name AS Department,
    o.WorkDate,
    o.Hours AS PlannedHours,
    o.OvertimeType,
    o.CreatedAt AS RequestDate
FROM HR.tbl_Overtime o
JOIN HR.tbl_Employees e ON o.EmployeeID = e.EmployeeID
JOIN HR.tbl_People p ON e.EmployeeID = p.PersonID
LEFT JOIN HR.tbl_Departments d ON e.DepartmentID = d.DepartmentID
WHERE o.Status = 'Planned';
GO