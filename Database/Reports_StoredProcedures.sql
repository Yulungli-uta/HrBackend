-- =============================================
-- Sistema de Reportes - Stored Procedures
-- Universidad Técnica de Ambato
-- =============================================

USE [HrSystem]
GO

-- =============================================
-- 1. Tabla de Auditoría de Reportes
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_ReportAudit' AND schema_id = SCHEMA_ID('HR'))
BEGIN
    CREATE TABLE [HR].[tbl_ReportAudit] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [UserEmail] NVARCHAR(255) NOT NULL,
        [ReportType] NVARCHAR(50) NOT NULL,
        [ReportFormat] NVARCHAR(10) NOT NULL CHECK (ReportFormat IN ('PDF', 'Excel')),
        [FiltersApplied] NVARCHAR(MAX), -- JSON
        [GeneratedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [FileSizeBytes] BIGINT NULL,
        [GenerationTimeMs] INT NULL,
        [ClientIp] NVARCHAR(50) NULL,
        [Success] BIT NOT NULL DEFAULT 1,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [FileName] NVARCHAR(255) NULL,
        
        INDEX IX_ReportAudit_UserId NONCLUSTERED (UserId),
        INDEX IX_ReportAudit_GeneratedAt NONCLUSTERED (GeneratedAt DESC),
        INDEX IX_ReportAudit_ReportType NONCLUSTERED (ReportType)
    );
    
    PRINT 'Tabla HR.tbl_ReportAudit creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Tabla HR.tbl_ReportAudit ya existe';
END
GO

-- =============================================
-- 2. SP: Reporte de Empleados
-- =============================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetEmployeesReport' AND schema_id = SCHEMA_ID('HR'))
    DROP PROCEDURE [HR].[sp_GetEmployeesReport];
GO

CREATE OR ALTER  PROCEDURE [HR].[sp_GetEmployeesReport]
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @DepartmentId INT = NULL,
    @FacultyId INT = NULL,
    @EmployeeType NVARCHAR(50) = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
CREATE OR ALTER  PROCEDURE [HR].[sp_GetEmployeesReport]
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @DepartmentId INT = NULL,
    @FacultyId INT = NULL,
    @EmployeeType NVARCHAR(50) = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.EmployeeId,
        CONCAT(p.FirstName, ' ', p.LastName) AS FullName,
        p.FirstName,
        --p.MiddleName,
        p.LastName,
        p.IDCard AS IdentificationNumber,
        e.Email,
        d.Name AS DepartmentName,
        d.Code AS DepartmentCode,
        --f.Name AS FacultyName,
        e.EmployeeType AS EmployeeType,
        e.IsActive,
        --ISNULL(c.BaseSalary, 0) AS BaseSalary,
        --ISNULL(c.NetSalary, 0) AS NetSalary,
        c.ContractTypeID,
        c.StartDate AS ContractStartDate,
        c.EndDate AS ContractEndDate,
        e.HireDate,
        e.CreatedAt,
        e.UpdatedAt
    FROM [HR].[tbl_Employees] e
    INNER JOIN [HR].[tbl_People] p ON e.PersonId = p.PersonId
    LEFT JOIN [HR].[tbl_Departments] d ON e.DepartmentId = d.DepartmentId
    --LEFT JOIN [HR].[tbl_Faculties] f ON d.FacultyId = f.Id
    LEFT JOIN (
        SELECT PersonID, --BaseSalary, 
                ContractTypeID, StartDate, EndDate
        FROM [HR].[tbl_Contracts]
        --WHERE IsActive = 1
    ) c ON e.PersonID = c.personid
    WHERE 
        (@StartDate IS NULL OR e.HireDate >= @StartDate)
        AND (@EndDate IS NULL OR e.HireDate <= @EndDate)
        AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        --AND (@FacultyId IS NULL OR d.FacultyId = @FacultyId)
        AND (@EmployeeType IS NULL OR e.EmployeeType = @EmployeeType)
        AND (@IsActive IS NULL OR e.IsActive = @IsActive)
    ORDER BY d.Name, FullName;
END
GO

PRINT 'SP HR.sp_GetEmployeesReport creado exitosamente';
GO

-- =============================================
-- 3. SP: Reporte de Asistencia
-- =============================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetAttendanceReport' AND schema_id = SCHEMA_ID('HR'))
    DROP PROCEDURE [HR].[sp_GetAttendanceReport];
GO

CREATE PROCEDURE [HR].[sp_GetAttendanceReport]
    @StartDate DATE,
    @EndDate DATE,
    @EmployeeId INT = NULL,
    @DepartmentId INT = NULL,
    @FacultyId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH AttendanceSummary AS (
        SELECT 
            a.AttendanceDate,
            a.EmployeeId,
            MIN(CASE WHEN a.PunchType = 'In' THEN a.PunchTime END) AS CheckIn,
            MAX(CASE WHEN a.PunchType = 'Out' THEN a.PunchTime END) AS CheckOut,
            a.AttendanceType
        FROM [HR].[tbl_Attendance] a
        WHERE a.AttendanceDate BETWEEN @StartDate AND @EndDate
        GROUP BY a.AttendanceDate, a.EmployeeId, a.AttendanceType
    )
    SELECT 
        a.AttendanceDate,
        e.Id AS EmployeeId,
        CONCAT(p.FirstName, ' ', p.LastName) AS EmployeeName,
        p.IdNumber AS IdentificationNumber,
        d.Name AS DepartmentName,
        f.Name AS FacultyName,
        a.CheckIn,
        a.CheckOut,
        CASE 
            WHEN a.CheckIn IS NOT NULL AND a.CheckOut IS NOT NULL THEN
                CAST(DATEDIFF(MINUTE, a.CheckIn, a.CheckOut) / 60.0 AS DECIMAL(10,2))
            ELSE NULL
        END AS HoursWorked,
        a.AttendanceType,
        CASE 
            WHEN a.CheckIn IS NULL THEN 'Sin Entrada'
            WHEN a.CheckOut IS NULL THEN 'Sin Salida'
            WHEN CAST(a.CheckIn AS TIME) > '08:30:00' THEN 'Tardanza'
            ELSE 'Normal'
        END AS Status
    FROM AttendanceSummary a
    INNER JOIN [HR].[tbl_Employees] e ON a.EmployeeId = e.EmployeeId
    INNER JOIN [HR].[tbl_People] p ON e.PersonId = p.PersonId
    LEFT JOIN [HR].[tbl_Departments] d ON e.DepartmentId = d.DepartmentId
    LEFT JOIN [HR].[tbl_Faculties] f ON d.FacultyId = f.Id
    WHERE 
        (@EmployeeId IS NULL OR a.EmployeeId = @EmployeeId)
        AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        AND (@FacultyId IS NULL OR d.FacultyId = @FacultyId)
    ORDER BY a.AttendanceDate DESC, EmployeeName;
END
GO

PRINT 'SP HR.sp_GetAttendanceReport creado exitosamente';
GO

-- =============================================
-- 4. SP: Reporte de Departamentos
-- =============================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetDepartmentsReport' AND schema_id = SCHEMA_ID('HR'))
    DROP PROCEDURE [HR].[sp_GetDepartmentsReport];
GO

CREATE PROCEDURE [HR].[sp_GetDepartmentsReport]
    @FacultyId INT = NULL,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        d.Id,
        d.Name AS DepartmentName,
        d.Code AS DepartmentCode,
        f.Name AS FacultyName,
        f.Code AS FacultyCode,
        d.IsActive,
        COUNT(DISTINCT e.Id) AS TotalEmployees,
        COUNT(DISTINCT CASE WHEN e.IsActive = 1 THEN e.Id END) AS ActiveEmployees,
        COUNT(DISTINCT CASE WHEN e.IsActive = 0 THEN e.Id END) AS InactiveEmployees,
        ISNULL(AVG(CASE WHEN c.IsActive = 1 THEN c.BaseSalary END), 0) AS AverageSalary,
        ISNULL(SUM(CASE WHEN c.IsActive = 1 THEN c.BaseSalary END), 0) AS TotalSalaries,
        ISNULL(MIN(CASE WHEN c.IsActive = 1 THEN c.BaseSalary END), 0) AS MinSalary,
        ISNULL(MAX(CASE WHEN c.IsActive = 1 THEN c.BaseSalary END), 0) AS MaxSalary,
        d.CreatedAt,
        d.UpdatedAt
    FROM [HR].[tbl_Departments] d
    LEFT JOIN [HR].[tbl_Faculties] f ON d.FacultyId = f.Id
    LEFT JOIN [HR].[tbl_Employees] e ON d.Id = e.DepartmentId
    LEFT JOIN [HR].[tbl_Contracts] c ON e.Id = c.EmployeeId AND c.IsActive = 1
    WHERE 
        (@FacultyId IS NULL OR d.FacultyId = @FacultyId)
        AND (@IncludeInactive = 1 OR d.IsActive = 1)
    GROUP BY 
        d.Id, d.Name, d.Code, f.Name, f.Code, d.IsActive, d.CreatedAt, d.UpdatedAt
    ORDER BY f.Name, d.Name;
END
GO

PRINT 'SP HR.sp_GetDepartmentsReport creado exitosamente';
GO

-- =============================================
-- 5. SP: Insertar Auditoría de Reporte
-- =============================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertReportAudit' AND schema_id = SCHEMA_ID('HR'))
    DROP PROCEDURE [HR].[sp_InsertReportAudit];
GO

CREATE PROCEDURE [HR].[sp_InsertReportAudit]
    @UserId UNIQUEIDENTIFIER,
    @UserEmail NVARCHAR(255),
    @ReportType NVARCHAR(50),
    @ReportFormat NVARCHAR(10),
    @FiltersApplied NVARCHAR(MAX) = NULL,
    @FileSizeBytes BIGINT = NULL,
    @GenerationTimeMs INT = NULL,
    @ClientIp NVARCHAR(50) = NULL,
    @Success BIT = 1,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @FileName NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [HR].[tbl_ReportAudit] (
        UserId, UserEmail, ReportType, ReportFormat, FiltersApplied,
        GeneratedAt, FileSizeBytes, GenerationTimeMs, ClientIp,
        Success, ErrorMessage, FileName
    )
    VALUES (
        @UserId, @UserEmail, @ReportType, @ReportFormat, @FiltersApplied,
        GETUTCDATE(), @FileSizeBytes, @GenerationTimeMs, @ClientIp,
        @Success, @ErrorMessage, @FileName
    );
    
    SELECT SCOPE_IDENTITY() AS AuditId;
END
GO

PRINT 'SP HR.sp_InsertReportAudit creado exitosamente';
GO

-- =============================================
-- 6. SP: Obtener Auditorías de Reportes
-- =============================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetReportAudits' AND schema_id = SCHEMA_ID('HR'))
    DROP PROCEDURE [HR].[sp_GetReportAudits];
GO

CREATE PROCEDURE [HR].[sp_GetReportAudits]
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @ReportType NVARCHAR(50) = NULL,
    @UserId UNIQUEIDENTIFIER = NULL,
    @Top INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Top)
        Id,
        UserId,
        UserEmail,
        ReportType,
        ReportFormat,
        FiltersApplied,
        GeneratedAt,
        FileSizeBytes,
        GenerationTimeMs,
        ClientIp,
        Success,
        ErrorMessage,
        FileName
    FROM [HR].[tbl_ReportAudit]
    WHERE 
        (@StartDate IS NULL OR GeneratedAt >= @StartDate)
        AND (@EndDate IS NULL OR GeneratedAt <= @EndDate)
        AND (@ReportType IS NULL OR ReportType = @ReportType)
        AND (@UserId IS NULL OR UserId = @UserId)
    ORDER BY GeneratedAt DESC;
END
GO

PRINT 'SP HR.sp_GetReportAudits creado exitosamente';
GO

PRINT '========================================';
PRINT 'TODOS LOS STORED PROCEDURES CREADOS EXITOSAMENTE';
PRINT '========================================';

-- =============================================
-- 7. SP: Resume de reporte de asistencia
-- =============================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetReportAttendanceSumary' AND schema_id = SCHEMA_ID('HR'))
    DROP PROCEDURE [HR].[sp_GetReportAttendanceSumary];
GO

CREATE OR ALTER PROCEDURE [HR].[sp_GetReportAttendanceSumary]
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @EmployeeID INT = NULL,
    @EmployeeType INT = NULL
	--@DepartmentId INT = NULL
    
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT tac.EmployeeID,
		ved.IDCard		,
		concat(ved.FirstName, ved.LastName) Nombre_completo,
		ved.EmployeeType,
		ved.ContractType,		
		tac.WorkDate, 
		tac.TotalWorkedMinutes, 
		tac.RegularMinutes,  
		tac.OvertimeMinutes, 
		tac.NightMinutes,
		tac.HolidayMinutes AS MinFeriado, 
		tac.RequiredMinutes AS MinTotLaboral, 
		tac.TardinessMin AS Atrazos,
		tac.FoodSubsidy AS Alimentacion, 
		tac.JustificationMinutes AS MinJustificacion
	FROM hr.tbl_AttendanceCalculations tac
	INNER JOIN Hr.vw_EmployeeDetails ved ON (tac.EmployeeID = ved.EmployeeID)
    WHERE 
		(@StartDate IS NULL OR tac.WorkDate >= @StartDate)
        AND (@EndDate IS NULL OR tac.WorkDate <= @EndDate)        
        AND (@EmployeeID IS NULL OR tac.EmployeeID = @EmployeeID)
		AND (@EmployeeType IS NULL OR ved.EmployeeType = @EmployeeType)
		--AND (@DepartmentId IS NULL OR tac.DepartmentId = @DepartmentId)
    ORDER BY tac.WorkDate DESC;
END
GO

PRINT 'SP HR.sp_GetReportAudits creado exitosamente';
GO
