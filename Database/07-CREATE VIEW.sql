CREATE OR ALTER VIEW HR.vw_EmployeeDetails AS
SELECT 
    p.PersonID      AS EmployeeID,
    p.FirstName, p.LastName, p.IDCard, E.Email,
    e.EmployeeType          AS EmployeeType,
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
) c
GO

PRINT (N'Create or alter view [HR].[vw_EmployeeComplete]')
GO
CREATE OR ALTER VIEW HR.vw_EmployeeComplete AS
SELECT 
    e.EmployeeID,
    p.FirstName,
    p.LastName,
    p.FirstName + ' ' + p.LastName AS FullName,
    p.IDCard,
    p.Email,
    p.Phone,
    p.BirthDate,
    p.Sex,
    p.Gender,
    p.Address,
    p.IsActive AS PersonIsActive,
    e.employeeType AS EmployeeType,
    rt.Name AS EmployeeTypeName,
    e.HireDate,
    e.IsActive AS EmployeeIsActive,
    d.Name AS Department,
    --f.Name AS Faculty,
    boss.FirstName + ' ' + boss.LastName AS ImmediateBoss,
    DATEDIFF(YEAR, e.HireDate, GETDATE()) AS YearsOfService,
    -- Información adicional de hoja de vida
    p.MaritalStatusTypeID,
    ms.Name AS MaritalStatus,
    p.EthnicityTypeID,
    eth.Name AS Ethnicity,
    p.BloodTypeTypeID,
    bt.Name AS BloodType,
    p.DisabilityPercentage,
    p.CONADISCard,
    -- Campos geográficos
    co.CountryName,
    pr.ProvinceName,
    ca.CantonName
FROM HR.tbl_Employees e
JOIN HR.tbl_People p ON e.EmployeeID = p.PersonID
LEFT JOIN HR.ref_Types rt ON e.employeeType = rt.TypeID
LEFT JOIN HR.tbl_Departments d ON e.DepartmentID = d.DepartmentID
--LEFT JOIN HR.tbl_Faculties f ON d.FacultyID = f.FacultyID
LEFT JOIN HR.tbl_Employees bossEmp ON e.ImmediateBossID = bossEmp.EmployeeID
LEFT JOIN HR.tbl_People boss ON bossEmp.EmployeeID = boss.PersonID
LEFT JOIN HR.ref_Types ms ON p.MaritalStatusTypeID = ms.TypeID
LEFT JOIN HR.ref_Types eth ON p.EthnicityTypeID = eth.TypeID
LEFT JOIN HR.ref_Types bt ON p.BloodTypeTypeID = bt.TypeID
LEFT JOIN HR.tbl_Countries co ON p.CountryID = co.CountryID
LEFT JOIN HR.tbl_Provinces pr ON p.ProvinceID = pr.ProvinceID
LEFT JOIN HR.tbl_Cantons ca ON p.CantonID = ca.CantonID
GO

PRINT (N'Create or alter view [HR].[vw_EmployeeDetails]')
GO
CREATE OR ALTER VIEW HR.vw_EmployeeDetails AS
SELECT 
    p.PersonID      AS EmployeeID,
    p.FirstName, p.LastName, p.IDCard, E.Email,
    e.EmployeeType          AS EmployeeType,
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
) c
GO