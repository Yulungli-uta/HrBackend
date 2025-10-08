/* MASTER ORDERED SCRIPT: HR schema
Generated on 2025-10-01 15:06:29.783158 */

-- =========================================
-- 1) DROP OBJECTS (safe order)
-- =========================================

-- === DROP OBJECTS FOR SCHEMA HR (safe-order) ===
SET NOCOUNT ON;

-- 1) Drop views
DECLARE @sql NVARCHAR(MAX)='';
SELECT @sql = STRING_AGG(CONCAT('IF OBJECT_ID(''', QUOTENAME(s.name)+'.'+QUOTENAME(v.name), ''',''V'') IS NOT NULL DROP VIEW ', QUOTENAME(s.name)+'.'+QUOTENAME(v.name), ';'), CHAR(10))
FROM sys.views v
JOIN sys.schemas s ON v.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);

-- 2) Drop triggers (table and database level) in HR
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('DROP TRIGGER ', QUOTENAME(s.name),'.',QUOTENAME(t.name),';'), CHAR(10))
FROM sys.triggers t
JOIN sys.schemas s ON t.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);

-- 3) Drop stored procedures
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('IF OBJECT_ID(''', QUOTENAME(s.name)+'.'+QUOTENAME(p.name), ''',''P'') IS NOT NULL DROP PROCEDURE ', QUOTENAME(s.name),'.',QUOTENAME(p.name), ';'), CHAR(10))
FROM sys.procedures p
JOIN sys.schemas s ON p.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);

-- 4) Drop functions
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('IF OBJECT_ID(''', QUOTENAME(s.name)+'.'+QUOTENAME(o.name), ''',''FN'') IS NOT NULL DROP FUNCTION ', QUOTENAME(s.name),'.',QUOTENAME(o.name), ';'), CHAR(10))
FROM sys.objects o
JOIN sys.schemas s ON o.schema_id=s.schema_id
WHERE s.name='HR' AND o.[type] IN ('FN','IF','TF');
EXEC(@sql);

-- 5) Drop foreign keys
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('ALTER TABLE ', QUOTENAME(SCHEMA_NAME(t.schema_id)),'.',QUOTENAME(t.name), ' DROP CONSTRAINT ', QUOTENAME(fk.name), ';'), CHAR(10))
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id=t.object_id
JOIN sys.schemas s ON t.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);

-- 6) Drop tables
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('DROP TABLE ', QUOTENAME(s.name),'.',QUOTENAME(t.name), ';'), CHAR(10))
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);
GO

-- =========================================
-- 2) CREATE TABLES
-- =========================================
-- HR.tbl_Countries
/* =========================================================
   TABLAS GEOGRÁFICAS
   ========================================================= */
-- Países
IF OBJECT_ID('HR.tbl_Countries','U') IS NOT NULL DROP TABLE HR.tbl_Countries;
CREATE TABLE HR.tbl_Countries (
    CountryID VARCHAR(10) NOT NULL,    
    CountryName VARCHAR(100) NOT NULL,
    Nationality VARCHAR(100) NULL,
    NationalityCode VARCHAR(5) NULL,
    AuxSIITH VARCHAR(5) NULL,
    AuxCEAACES VARCHAR(5) NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_Provinces
-- Provincias
IF OBJECT_ID('HR.tbl_Provinces','U') IS NOT NULL DROP TABLE HR.tbl_Provinces;
CREATE TABLE HR.tbl_Provinces (
    ProvinceID VARCHAR(10) NOT NULL,
    CountryID VARCHAR(10) NOT NULL,
    ProvinceName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_Cantons
-- Cantones
IF OBJECT_ID('HR.tbl_Cantons','U') IS NOT NULL DROP TABLE HR.tbl_Cantons;
CREATE TABLE HR.tbl_Cantons (
    CantonID VARCHAR(10) NOT NULL,
    ProvinceID VARCHAR(10) NOT NULL,
    CantonName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_Addresses
/* =========================================================
   TABLAS DE HISTORIA DE VIDA
   ========================================================= */
-- Direcciones
IF OBJECT_ID('HR.tbl_Addresses','U') IS NOT NULL DROP TABLE HR.tbl_Addresses;
CREATE TABLE HR.tbl_Addresses (
    AddressID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    AddressTypeID INT NOT NULL,
    CountryID VARCHAR(10) NOT NULL,
    ProvinceID VARCHAR(10) NOT NULL,
    CantonID VARCHAR(10) NOT NULL,
    Parish VARCHAR(100) NULL,
    Neighborhood VARCHAR(100) NULL,
    MainStreet VARCHAR(100) NOT NULL,
    SecondaryStreet VARCHAR(100) NULL,
    HouseNumber VARCHAR(20) NULL,
    Reference VARCHAR(255) NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_Institutions
-- Instituciones
IF OBJECT_ID('HR.tbl_Institutions','U') IS NOT NULL DROP TABLE HR.tbl_Institutions;
CREATE TABLE HR.tbl_Institutions (
    InstitutionID INT IDENTITY(1,1) NOT NULL,
    Name VARCHAR(200) NOT NULL,
    InstitutionTypeID INT NOT NULL,
    CountryID VARCHAR(10) NOT NULL,
    ProvinceID VARCHAR(10) NOT NULL,
    CantonID VARCHAR(10) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_EducationLevels
-- Niveles de Estudio
IF OBJECT_ID('HR.tbl_EducationLevels','U') IS NOT NULL DROP TABLE HR.tbl_EducationLevels;
CREATE TABLE HR.tbl_EducationLevels (
    EducationID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    EducationLevelTypeID INT NOT NULL,
    InstitutionID INT NOT NULL,
    Title VARCHAR(150) NOT NULL,
    Specialty VARCHAR(100) NULL,
    StartDate DATE NULL,
    EndDate DATE NULL,
    Grade VARCHAR(50) NULL,
    Location VARCHAR(100) NULL,
    Score DECIMAL(5,2) NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_EmergencyContacts
-- Contactos de Emergencia
IF OBJECT_ID('HR.tbl_EmergencyContacts','U') IS NOT NULL DROP TABLE HR.tbl_EmergencyContacts;
CREATE TABLE HR.tbl_EmergencyContacts (
    ContactID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Identification VARCHAR(20) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    RelationshipTypeID INT NOT NULL,
    Address VARCHAR(255) NULL,
    Phone VARCHAR(30) NULL,
    Mobile VARCHAR(30) NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_CatastrophicIllnesses
-- Enfermedades Catastróficas
IF OBJECT_ID('HR.tbl_CatastrophicIllnesses','U') IS NOT NULL DROP TABLE HR.tbl_CatastrophicIllnesses;
CREATE TABLE HR.tbl_CatastrophicIllnesses (
    IllnessID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Illness VARCHAR(150) NOT NULL,
    IESSNumber VARCHAR(50) NULL,
    SubstituteName VARCHAR(100) NULL,
    IllnessTypeID INT NOT NULL,
    CertificateNumber VARCHAR(50) NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_FamilyBurden
-- Carga Familiar
IF OBJECT_ID('HR.tbl_FamilyBurden','U') IS NOT NULL DROP TABLE HR.tbl_FamilyBurden;
CREATE TABLE HR.tbl_FamilyBurden (
    BurdenID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    DependentID VARCHAR(20) NOT NULL,
    IdentificationTypeID INT NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    BirthDate DATE NOT NULL,
    DisabilityTypeID INT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_Trainings
-- Capacitaciones
IF OBJECT_ID('HR.tbl_Trainings','U') IS NOT NULL DROP TABLE HR.tbl_Trainings;
CREATE TABLE HR.tbl_Trainings (
    TrainingID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Location VARCHAR(100) NULL,
    Title VARCHAR(200) NOT NULL,
    Institution VARCHAR(150) NOT NULL,
    KnowledgeAreaTypeID INT NULL,
    EventTypeID INT NULL,
    CertifiedBy VARCHAR(150) NULL,
    CertificateTypeID INT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Hours INT NOT NULL,
    ApprovalTypeID INT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_WorkExperiences
-- Experiencia Laboral
IF OBJECT_ID('HR.tbl_WorkExperiences','U') IS NOT NULL DROP TABLE HR.tbl_WorkExperiences;
CREATE TABLE HR.tbl_WorkExperiences (
    WorkExpID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    CountryID VARCHAR(10) NULL,
    Company VARCHAR(150) NOT NULL,
    InstitutionTypeID INT NULL,
    EntryReason VARCHAR(200) NULL,
    ExitReason VARCHAR(200) NULL,
    Position VARCHAR(120) NOT NULL,
    InstitutionAddress VARCHAR(255) NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    ExperienceTypeID INT NULL,
    IsCurrent BIT NOT NULL DEFAULT(0),
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_BankAccounts
-- Cuentas Bancarias
IF OBJECT_ID('HR.tbl_BankAccounts','U') IS NOT NULL DROP TABLE HR.tbl_BankAccounts;
CREATE TABLE HR.tbl_BankAccounts (
    AccountID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    FinancialInstitution VARCHAR(150) NOT NULL,
    AccountTypeID INT NOT NULL,
    AccountNumber VARCHAR(50) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_Publications
-- Publicaciones
IF OBJECT_ID('HR.tbl_Publications','U') IS NOT NULL DROP TABLE HR.tbl_Publications;
CREATE TABLE HR.tbl_Publications (
    PublicationID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Location VARCHAR(100) NULL,
    PublicationTypeID INT NULL,
    IsIndexed BIT NULL,
    JournalTypeID INT NULL,
    ISSN_ISBN VARCHAR(20) NULL,
    JournalName VARCHAR(200) NULL,
    JournalNumber VARCHAR(50) NULL,
    Volume VARCHAR(50) NULL,
    Pages VARCHAR(20) NULL,
    KnowledgeAreaTypeID INT NULL,
    SubAreaTypeID INT NULL,
    AreaTypeID INT NULL,
    Title VARCHAR(300) NOT NULL,
    OrganizedBy VARCHAR(150) NULL,
    EventName VARCHAR(200) NULL,
    EventEdition VARCHAR(50) NULL,
    PublicationDate DATE NULL,
    UTAffiliation BIT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.tbl_Books
-- Libros
IF OBJECT_ID('HR.tbl_Books','U') IS NOT NULL DROP TABLE HR.tbl_Books;
CREATE TABLE HR.tbl_Books (
    BookID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Title VARCHAR(300) NOT NULL,
    PeerReviewed BIT NULL,
    ISBN VARCHAR(20) NULL,
    Publisher VARCHAR(200) NULL,
    CountryID VARCHAR(10) NULL,
    City VARCHAR(100) NULL,
    KnowledgeAreaTypeID INT NULL,
    SubAreaTypeID INT NULL,
    AreaTypeID INT NULL,
    VolumeCount INT NULL,
    ParticipationTypeID INT NULL,
    PublicationDate DATE NULL,
    UTAffiliation BIT NULL,
    UTASponsorship BIT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- HR.ref_Types
/*ALTER TABLE dbutasystem.HR.tbl_jobs
ADD IsActive BIT NOT NULL DEFAULT(1);*/

ALTER TABLE dbutasystem.HR.tbl_PersonnelMovements
ADD IsActive BIT NOT NULL DEFAULT(1);

-------------------------------------------------------------------------------
-- 2) ALTERs: PK, UNIQUE y FKs
-------------------------------------------------------------------------------

-- PKs
-- Tabla de tipos
ALTER TABLE HR.ref_Types ADD CONSTRAINT PK_ref_Types PRIMARY KEY (TypeID);
ALTER TABLE HR.tbl_People              ADD CONSTRAINT PK_People PRIMARY KEY (PersonID);
--ALTER TABLE HR.tbl_Faculties           ADD CONSTRAINT PK_Faculties PRIMARY KEY (FacultyID);
ALTER TABLE HR.tbl_Departments         ADD CONSTRAINT PK_Departments PRIMARY KEY (DepartmentID);
ALTER TABLE HR.tbl_Employees           ADD CONSTRAINT PK_Employees PRIMARY KEY (EmployeeID);
ALTER TABLE HR.tbl_Schedules           ADD CONSTRAINT PK_Schedules PRIMARY KEY (ScheduleID);
ALTER TABLE HR.tbl_EmployeeSchedules   ADD CONSTRAINT PK_EmployeeSchedules PRIMARY KEY (EmpScheduleID);
ALTER TABLE HR.tbl_Contracts           ADD CONSTRAINT PK_Contracts PRIMARY KEY (ContractID);
ALTER TABLE HR.tbl_SalaryHistory       ADD CONSTRAINT PK_SalaryHistory PRIMARY KEY (SalaryHistoryID);
ALTER TABLE HR.tbl_PermissionTypes     ADD CONSTRAINT PK_PermissionTypes PRIMARY KEY (TypeID);
ALTER TABLE HR.tbl_Vacations           ADD CONSTRAINT PK_Vacations PRIMARY KEY (VacationID);
ALTER TABLE HR.tbl_Permissions         ADD CONSTRAINT PK_Permissions PRIMARY KEY (PermissionID);
ALTER TABLE HR.tbl_AttendancePunches   ADD CONSTRAINT PK_AttendancePunches PRIMARY KEY (PunchID);
ALTER TABLE HR.tbl_PunchJustifications ADD CONSTRAINT PK_PunchJustifications PRIMARY KEY (PunchJustID);
ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT PK_AttendanceCalculations PRIMARY KEY (CalculationID);
ALTER TABLE HR.tbl_OvertimeConfig      ADD CONSTRAINT PK_OvertimeConfig PRIMARY KEY (OvertimeType);
ALTER TABLE HR.tbl_Overtime            ADD CONSTRAINT PK_Overtime PRIMARY KEY (OvertimeID);
ALTER TABLE HR.tbl_TimeRecoveryPlans   ADD CONSTRAINT PK_TimeRecoveryPlans PRIMARY KEY (RecoveryPlanID);
ALTER TABLE HR.tbl_TimeRecoveryLogs    ADD CONSTRAINT PK_TimeRecoveryLogs PRIMARY KEY (RecoveryLogID);
ALTER TABLE HR.tbl_Subrogations        ADD CONSTRAINT PK_Subrogations PRIMARY KEY (SubrogationID);
ALTER TABLE HR.tbl_PersonnelMovements  ADD CONSTRAINT PK_PersonnelMovements PRIMARY KEY (MovementID);
ALTER TABLE HR.tbl_Payroll             ADD CONSTRAINT PK_Payroll PRIMARY KEY (PayrollID);
ALTER TABLE HR.tbl_PayrollLines        ADD CONSTRAINT PK_PayrollLines PRIMARY KEY (PayrollLineID);
ALTER TABLE HR.tbl_Audit               ADD CONSTRAINT PK_Audit PRIMARY KEY (AuditID);
--ALTER TABLE HR.tbl_Departments  	  ADD CONSTRAINT PK_Departments PRIMARY KEY (DepartmentID);

ALTER TABLE HR.tbl_Departments 		   ADD CONSTRAINT UK_Departments_Code UNIQUE (Code);




-- UNIQUEs
ALTER TABLE HR.tbl_People      ADD CONSTRAINT UQ_People_IDCard UNIQUE(IDCard);
ALTER TABLE HR.tbl_People      ADD CONSTRAINT UQ_People_Email  UNIQUE(Email);
--ALTER TABLE HR.tbl_Departments ADD CONSTRAINT UQ_Department UNIQUE(FacultyID, Name);
ALTER TABLE HR.tbl_Payroll     ADD CONSTRAINT UQ_Payroll UNIQUE(EmployeeID, Period);

-- FKs
--ALTER TABLE HR.tbl_Departments
  --  ADD CONSTRAINT FK_Departments_Faculty
    --    FOREIGN KEY (FacultyID) REFERENCES HR.tbl_Faculties(FacultyID);

--ALTER TABLE HR.tbl_Faculties
  --  ADD CONSTRAINT FK_Faculties_Dean FOREIGN KEY (DeanEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
ALTER TABLE HR.tbl_Departments 
	ADD CONSTRAINT FK_Departments_Parent FOREIGN KEY (ParentID) REFERENCES HR.tbl_Departments(DepartmentID);  

ALTER TABLE HR.tbl_Employees
    ADD CONSTRAINT FK_Employees_Persons FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_People(PersonID) ON DELETE CASCADE;

ALTER TABLE HR.tbl_Employees
    ADD CONSTRAINT FK_Employees_Department FOREIGN KEY (DepartmentID) REFERENCES HR.tbl_Departments(DepartmentID);

ALTER TABLE HR.tbl_Employees
    ADD CONSTRAINT FK_Employees_Boss FOREIGN KEY (ImmediateBossID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_EmployeeSchedules
    ADD CONSTRAINT FK_EmpSchedules_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_EmpSchedules_Schedule FOREIGN KEY (ScheduleID) REFERENCES HR.tbl_Schedules(ScheduleID);

ALTER TABLE HR.tbl_Contracts
    ADD CONSTRAINT FK_Contracts_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_SalaryHistory
    ADD CONSTRAINT FK_SalaryHistory_Contract FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts(ContractID);

ALTER TABLE HR.tbl_Vacations
    ADD CONSTRAINT FK_Vacations_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_Permissions
    ADD CONSTRAINT FK_Permissions_Employee  FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_Permissions_Type      FOREIGN KEY (PermissionTypeID) REFERENCES HR.tbl_PermissionTypes(TypeID),
        CONSTRAINT FK_Permissions_Vacation  FOREIGN KEY (VacationID) REFERENCES HR.tbl_Vacations(VacationID);

ALTER TABLE HR.tbl_AttendancePunches
    ADD CONSTRAINT FK_Punch_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_PunchJustifications
    ADD --CONSTRAINT FK_PJ_Punch  FOREIGN KEY (PunchID) REFERENCES HR.tbl_AttendancePunches(PunchID) ON DELETE CASCADE,
        CONSTRAINT FK_PJ_Boss   FOREIGN KEY (BossEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_AttendanceCalculations
    ADD CONSTRAINT FK_AttCalc_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_Overtime
    ADD CONSTRAINT FK_OT_Employee    FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_OT_Type        FOREIGN KEY (OvertimeType) REFERENCES HR.tbl_OvertimeConfig(OvertimeType);

ALTER TABLE HR.tbl_TimeRecoveryPlans
    ADD CONSTRAINT FK_TRP_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_TimeRecoveryLogs
    ADD CONSTRAINT FK_TRL_Plan FOREIGN KEY (RecoveryPlanID) REFERENCES HR.tbl_TimeRecoveryPlans(RecoveryPlanID);

ALTER TABLE HR.tbl_Subrogations
    ADD CONSTRAINT FK_Subro_Titular  FOREIGN KEY (SubrogatedEmployeeID)  REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_Subro_Subroga  FOREIGN KEY (SubrogatingEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_Subro_Perm     FOREIGN KEY (PermissionID) REFERENCES HR.tbl_Permissions(PermissionID),
        CONSTRAINT FK_Subro_Vac      FOREIGN KEY (VacationID)   REFERENCES HR.tbl_Vacations(VacationID);

ALTER TABLE HR.tbl_PersonnelMovements
    ADD CONSTRAINT FK_Move_Employee  FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_Move_Contract  FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts(ContractID),
        CONSTRAINT FK_Move_Origin    FOREIGN KEY (OriginDepartmentID) REFERENCES HR.tbl_Departments(DepartmentID),
        CONSTRAINT FK_Move_Dest      FOREIGN KEY (DestinationDepartmentID) REFERENCES HR.tbl_Departments(DepartmentID);

ALTER TABLE HR.tbl_Payroll
    ADD CONSTRAINT FK_Payroll_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_PayrollLines
    ADD CONSTRAINT FK_PL_Payroll FOREIGN KEY (PayrollID) REFERENCES HR.tbl_Payroll(PayrollID) ON DELETE CASCADE;

-- Checks adicionales
ALTER TABLE HR.tbl_TimeRecoveryPlans
    ADD CONSTRAINT CK_TRP_AtLeastPlannedMinutes CHECK (DATEDIFF(MINUTE, FromTime, ToTime) >= OwedMinutes AND DATEDIFF(MINUTE, FromTime, ToTime) >= 60);
ALTER TABLE HR.tbl_Subrogations
    ADD CONSTRAINT CK_Subro_Dates CHECK (EndDate >= StartDate);
	
	/*HOJA DE VIDA */
	
	ALTER TABLE [HR].[tbl_AttendancePunches] 
	ALTER COLUMN [CreatedAt] datetime2;

	ALTER TABLE [HR].[tbl_AttendancePunches] 
	ALTER COLUMN [PunchTime] datetime2;
	
	--ALTER TABLE [HR].[tbl_Employees] 
	--ALTER COLUMN [type] INT;
	
	IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningEmployees_Status')
    ALTER TABLE HR.tbl_Departments 
    ADD CONSTRAINT FK_Departments_RefType
    FOREIGN KEY (DepartmentType) REFERENCES HR.ref_Types(TypeID);
	
	
	ALTER TABLE [HR].[tbl_Employees]
    ADD CONSTRAINT FK_Employees_type  FOREIGN KEY (type)  REFERENCES HR.ref_Types(TypeID);
	
	ALTER TABLE dbutasystem.[HR].[tbl_contracts]
    ADD CONSTRAINT FK_contract_type  FOREIGN KEY (contractType)  REFERENCES HR.ref_Types(TypeID);
	
	ALTER TABLE dbutasystem.HR.tbl_PersonnelMovements
	ADD CONSTRAINT FK_person_Job  FOREIGN KEY (JobID)  REFERENCES HR.tbl_jobs(JobID);
/*IF OBJECT_ID('HR.ref_Types','U') IS NOT NULL DROP TABLE HR.ref_Types;
CREATE TABLE HR.ref_Types (
    TypeID INT IDENTITY(1,1) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO*/


/*FIN */

-- Foreign Key hacia la tabla de tipos
ALTER TABLE [HR].[tbl_PunchJustifications] 
ADD CONSTRAINT [FK_PunchJustifications_TypeID] 
FOREIGN KEY ([JustificationTypeID]) REFERENCES [HR].[ref_Types] ([TypeID])
GO

-- HR.tbl_People
-- Personas
IF OBJECT_ID('HR.tbl_People','U') IS NOT NULL DROP TABLE HR.tbl_People;
CREATE TABLE HR.tbl_People (
    PersonID            INT IDENTITY(1,1) NOT NULL,
    FirstName           VARCHAR(100)   NOT NULL,
    LastName            VARCHAR(100)   NOT NULL,
	IdentType			INT    			NULL,
    IDCard              VARCHAR(20)    NOT NULL,
    Email               VARCHAR(150)   NOT NULL,
    Phone               VARCHAR(30)    NULL,
    BirthDate           DATE           NULL,
    --Sex                 VARCHAR(50)    NULL, --CHECK (Sex IN ('M','F','O')),
    --Gender              VARCHAR(50)    NULL,
	Sex                 INT    NULL,
    Gender              INT    NULL,
    Disability          VARCHAR(200)   NULL,
    Address             VARCHAR(255)   NULL,
    IsActive            BIT            NOT NULL DEFAULT(1),
    CreatedAt           DATETIME2(0)   NOT NULL DEFAULT(SYSDATETIME()),
    UpdatedAt           DATETIME2(0)   NULL,
	MaritalStatusTypeID INT NULL,
    MilitaryCard 		VARCHAR(50) NULL,
    MotherName 			VARCHAR(100) NULL,
    FatherName 			VARCHAR(100) NULL,
    CountryID 			INT NULL,
    ProvinceID 			INT NULL,
    CantonID 			INT NULL,
    YearsOfResidence 	INT NULL,
    EthnicityTypeID 	INT NULL,
    BloodTypeTypeID 	INT NULL,
    SpecialNeedsTypeID 	INT NULL,
    DisabilityPercentage DECIMAL(5,2) NULL,
    CONADISCard 		VARCHAR(50) NULL,
    RowVersion          ROWVERSION
);
GO

-- HR.tbl_Faculties
-- Facultades
/*IF OBJECT_ID('HR.tbl_Faculties','U') IS NOT NULL DROP TABLE HR.tbl_Faculties;
CREATE TABLE HR.tbl_Faculties(
    FacultyID       INT IDENTITY(1,1) NOT NULL,
    Name            VARCHAR(120)    NOT NULL,
    DeanEmployeeID  INT             NULL,
    IsActive        BIT             NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2(0)    NULL,
    RowVersion      ROWVERSION
);
GO

-- HR.tbl_Departments
-- Departamentos
IF OBJECT_ID('HR.tbl_Departments','U') IS NOT NULL DROP TABLE HR.tbl_Departments;
CREATE TABLE HR.tbl_Departments(
    DepartmentID    INT IDENTITY(1,1) NOT NULL,
    FacultyID       INT             NULL,
    Name            VARCHAR(120)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2(0)    NULL,
    RowVersion      ROWVERSION
);
GO*/

IF OBJECT_ID('HR.tbl_Departments','U') IS NOT NULL DROP TABLE HR.tbl_Departments;
CREATE TABLE HR.tbl_Departments(
    DepartmentID    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ParentID        INT             NULL,
    Code            VARCHAR(20)     NOT NULL UNIQUE, -- Código único (ej: "D001", "FING")
    Name            VARCHAR(120)    NOT NULL,
    ShortName       VARCHAR(50)     NULL, -- Nombre corto (ej: "Ingeniería")
    DepartmentType  INT     		NOT NULL, 
    Email           VARCHAR(100)    NULL,
    Phone           VARCHAR(20)     NULL,
    Location        VARCHAR(200)    NULL, -- Ubicación física
    DeanDirector    INT             NULL, -- FK a tabla de Empleados (opcional)
    BudgetCode      VARCHAR(30)     NULL,
    Dlevel          INT             NULL, --nivel jerarquico
    IsActive        BIT             NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2(0)    NULL,
    RowVersion      ROWVERSION
);

-- Empleados (vincula a Persona)
IF OBJECT_ID('HR.tbl_Employees','U') IS NOT NULL DROP TABLE HR.tbl_Employees;
CREATE TABLE HR.tbl_Employees(
    EmployeeID          INT           NOT NULL, -- = PersonID
    --Type                VARCHAR(40)   NOT NULL CHECK (Type IN ('Teacher_LOSE','Administrative_LOSEP','Employee_CT','Coordinator')),
	TYPE				INT			  NULL,
    DepartmentID        INT           NULL,
    ImmediateBossID     INT           NULL,
    HireDate            DATE          NOT NULL,
	email      			NVARCHAR(150) null,
    IsActive            BIT           NOT NULL DEFAULT(1),
    CreatedBy           INT           NULL,
    CreatedAt           DATETIME2(0)  NOT NULL DEFAULT(SYSDATETIME()),
    UpdatedBy           INT           NULL,
    UpdatedAt           DATETIME2(0)  NULL,
    RowVersion          ROWVERSION
);
GO

-- HR.tbl_Schedules
-- Schedules
IF OBJECT_ID('HR.tbl_Schedules','U') IS NOT NULL DROP TABLE HR.tbl_Schedules;
CREATE TABLE HR.tbl_Schedules(
    ScheduleID          INT IDENTITY(1,1) NOT NULL,
    Description         VARCHAR(120) NOT NULL,
    EntryTime           TIME         NOT NULL,
    ExitTime            TIME         NOT NULL,
    WorkingDays         VARCHAR(20)  NOT NULL, -- '1,2,3,4,5'
    RequiredHoursPerDay DECIMAL(5,2) NOT NULL,
    HasLunchBreak       BIT          NOT NULL DEFAULT(1),
    LunchStart          TIME         NULL,
    LunchEnd            TIME         NULL,
    IsRotating          BIT          NOT NULL DEFAULT(0),
    RotationPattern     VARCHAR(120) NULL,
    CreatedAt           DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME()),
    UpdatedAt           DATETIME2(0) NULL,
    RowVersion          ROWVERSION
);
GO

-- HR.tbl_EmployeeSchedules
-- Asignación de horarios por vigencia
IF OBJECT_ID('HR.tbl_EmployeeSchedules','U') IS NOT NULL DROP TABLE HR.tbl_EmployeeSchedules;
CREATE TABLE HR.tbl_EmployeeSchedules(
    EmpScheduleID   INT IDENTITY(1,1) NOT NULL,
    EmployeeID      INT NOT NULL,
    ScheduleID      INT NOT NULL,
    ValidFrom       DATE NOT NULL,
    ValidTo         DATE NULL, -- NULL = vigente
    CreatedAt       DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME()),
    RowVersion      ROWVERSION
);
GO

-- HR.tbl_Contracts
-- Contratos
IF OBJECT_ID('HR.tbl_Contracts','U') IS NOT NULL DROP TABLE HR.tbl_Contracts;
CREATE TABLE HR.tbl_Contracts(
    ContractID      INT IDENTITY(1,1) NOT NULL,
    EmployeeID      INT           NOT NULL,
    ContractType    INT   		  NOT NULL,
	JobID		
    StartDate       DATE          NOT NULL,
    EndDate         DATE          NULL,

	DocumentNum 	NVARCHAR(50)	  NOT NULL, --NUMERO DE DOCUMENTO
	Motivation		NVARCHAR(MAX) NULL, 	--MOTIVACION
	BudgetItem		NVARCHAR(50)  NULL,		--PARTIDA PRESUPUESTARIA
	Grade			INT 			NULL,	--GRADO
	GovernanceLevel NVARCHAR(MAX) NULL,  --NIVEL DE GESTION
	Workplace		NVARCHAR(500) NULL,
	
    BaseSalary      DECIMAL(12,2) NOT NULL,
    CreatedBy       INT           NULL,
    CreatedAt       DATETIME2(0)  NOT NULL DEFAULT(SYSDATETIME()),
    UpdatedBy       INT           NULL,
    UpdatedAt       DATETIME2(0)  NULL,
    RowVersion      ROWVERSION
);
GO

-- HR.tbl_SalaryHistory
-- Histórico de salarios
IF OBJECT_ID('HR.tbl_SalaryHistory','U') IS NOT NULL DROP TABLE HR.tbl_SalaryHistory;
CREATE TABLE HR.tbl_SalaryHistory(
    SalaryHistoryID INT IDENTITY(1,1) NOT NULL,
    ContractID      INT           NOT NULL,
    OldSalary       DECIMAL(12,2) NOT NULL,
    NewSalary       DECIMAL(12,2) NOT NULL,
    ChangedBy       SYSNAME       NOT NULL DEFAULT SUSER_SNAME(),
    ChangedAt       DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    Reason          VARCHAR(300)  NULL
);
GO

-- HR.tbl_PermissionTypes
-- Tipos de permisos
IF OBJECT_ID('HR.tbl_PermissionTypes','U') IS NOT NULL DROP TABLE HR.tbl_PermissionTypes;
CREATE TABLE HR.tbl_PermissionTypes(
    TypeID              INT IDENTITY(1,1) NOT NULL,
    Name                VARCHAR(80) NOT NULL,
    DeductsFromVacation BIT NOT NULL DEFAULT(0),
    RequiresApproval    BIT NOT NULL DEFAULT(1),
    MaxDays             INT NULL
);
GO

-- HR.tbl_Vacations
-- Vacaciones
IF OBJECT_ID('HR.tbl_Vacations','U') IS NOT NULL DROP TABLE HR.tbl_Vacations;
CREATE TABLE HR.tbl_Vacations(
    VacationID      INT IDENTITY(1,1) NOT NULL,
    EmployeeID      INT NOT NULL,
    StartDate       DATE NOT NULL,
    EndDate         DATE NOT NULL,
    DaysGranted     INT  NOT NULL,
    DaysTaken       INT  NOT NULL DEFAULT(0),
    Status          VARCHAR(20) NOT NULL DEFAULT('Planned') CHECK (Status IN ('Planned','InProgress','Completed','Canceled')),
    CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,
    RowVersion      ROWVERSION
);
GO

-- HR.tbl_Permissions
-- Permisos
IF OBJECT_ID('HR.tbl_Permissions','U') IS NOT NULL DROP TABLE HR.tbl_Permissions;
CREATE TABLE HR.tbl_Permissions(
    PermissionID        INT IDENTITY(1,1) NOT NULL,
    EmployeeID          INT NOT NULL,
    PermissionTypeID    INT NOT NULL,
    StartDate           DATE NOT NULL,
    EndDate             DATE NOT NULL,
    ChargedToVacation   BIT  NOT NULL DEFAULT(0),
    ApprovedBy          INT  NULL,
    Justification       VARCHAR(MAX) NULL,
    RequestDate         DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME()),
    Status              VARCHAR(20) NOT NULL DEFAULT('Pending') CHECK (Status IN ('Pending','Approved','Rejected')),
    VacationID          INT NULL,
    RowVersion          ROWVERSION
);
GO

-- HR.tbl_AttendancePunches
-- Asistencia / Picadas
IF OBJECT_ID('HR.tbl_AttendancePunches','U') IS NOT NULL DROP TABLE HR.tbl_AttendancePunches;
CREATE TABLE HR.tbl_AttendancePunches(
    PunchID         INT IDENTITY(1,1) NOT NULL,
    EmployeeID      INT NOT NULL,
    PunchTime       DATETIME2(0) NOT NULL,
    PunchType       VARCHAR(10) NOT NULL CHECK (PunchType IN ('In','Out')),
    DeviceID        VARCHAR(60) NULL,
    Longitude       DECIMAL(10,7) NULL,
    Latitude        DECIMAL(10,7) NULL,
    CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    RowVersion      ROWVERSION
);
GO

-- HR.tbl_PunchJustifications
-- Justificación de picadas
IF OBJECT_ID('HR.tbl_PunchJustifications','U') IS NOT NULL DROP TABLE HR.tbl_PunchJustifications;
/*CREATE TABLE HR.tbl_PunchJustifications(
    PunchJustID     INT IDENTITY(1,1) NOT NULL,
    PunchID         INT NOT NULL,
    BossEmployeeID  INT NOT NULL,
    Reason          VARCHAR(500) NOT NULL,
    Approved        BIT NOT NULL DEFAULT(0),
    ApprovedAt      DATETIME2(0) NULL
);*/
CREATE TABLE [HR].[tbl_PunchJustifications](
	[PunchJustID] [int] IDENTITY(1,1) NOT NULL,
	--[PunchID] [int] NULL, -- Ahora es nullable para justificaciones de día completo
	[EmployeeID] [int] NOT NULL, -- ID del empleado que solicita la justificación
	[BossEmployeeID] [int] NOT NULL, -- ID del jefe que aprueba
	[JustificationTypeID] [int] NOT NULL, -- FK a HR.ref_Types
	[StartDateTime] [datetime2](0) NULL, -- Para justificaciones de rango de tiempo
	[EndDateTime] [datetime2](0) NULL, -- Para justificaciones de rango de tiempo
	[JustificationDate] [date] NULL, -- Para justificaciones de día completo
	[Reason] [nvarchar](500) NOT NULL,
	[HoursRequested] [decimal](4,2) NULL, -- Para justificaciones de horas
	[Approved] [bit] NOT NULL DEFAULT 0,
	[ApprovedAt] [datetime2](0) NULL,
	[CreatedAt] [datetime2](0) NOT NULL DEFAULT GETDATE(),
	[CreatedBy] [int] NOT NULL, -- ID del usuario que crea la justificación
	[Comments] [nvarchar](1000) NULL, -- Comentarios del aprobador
	[Status] [nvarchar](20) NOT NULL DEFAULT 'PENDING', -- 'PENDING', 'APPROVED', 'REJECTED'
 CONSTRAINT [PK_PunchJustifications] PRIMARY KEY CLUSTERED 
(
	[PunchJustID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- HR.tbl_AttendanceCalculations
-- Agregados de asistencia
IF OBJECT_ID('HR.tbl_AttendanceCalculations','U') IS NOT NULL DROP TABLE HR.tbl_AttendanceCalculations;
CREATE TABLE HR.tbl_AttendanceCalculations(
    CalculationID       INT IDENTITY(1,1) NOT NULL,
    EmployeeID          INT NOT NULL,
    WorkDate            DATE NOT NULL,
    FirstPunchIn        DATETIME2(0) NULL,
    LastPunchOut        DATETIME2(0) NULL,
    TotalWorkedMinutes  INT NOT NULL DEFAULT(0),
    RegularMinutes      INT NOT NULL DEFAULT(0),
    OvertimeMinutes     INT NOT NULL DEFAULT(0),
    NightMinutes        INT NOT NULL DEFAULT(0),
    HolidayMinutes      INT NOT NULL DEFAULT(0),
    Status              VARCHAR(12) NOT NULL DEFAULT('Pending') CHECK (Status IN ('Pending','Approved'))
);
GO

-- HR.tbl_OvertimeConfig
-- Config de horas extra
IF OBJECT_ID('HR.tbl_OvertimeConfig','U') IS NOT NULL DROP TABLE HR.tbl_OvertimeConfig;
CREATE TABLE HR.tbl_OvertimeConfig(
    OvertimeType    VARCHAR(50) NOT NULL, -- PK luego
    Factor          DECIMAL(5,2) NOT NULL,
    Description     VARCHAR(200) NULL
);
GO

-- HR.tbl_Overtime
-- Horas extra
/* IF OBJECT_ID('HR.tbl_Overtime','U') IS NOT NULL DROP TABLE HR.tbl_Overtime;
CREATE TABLE HR.tbl_Overtime(
    OvertimeID      INT IDENTITY(1,1) NOT NULL,
    EmployeeID      INT NOT NULL,
    WorkDate        DATE NOT NULL,
    OvertimeType    VARCHAR(50) NOT NULL,
    Hours           DECIMAL(5,2) NOT NULL CHECK (Hours > 0),
    Status          VARCHAR(20) NOT NULL DEFAULT('Planned') CHECK (Status IN ('Planned','Verified','Rejected','Paid')),
    ApprovedBy      INT NULL,
    SecondApprover  INT NULL,
    Factor          DECIMAL(5,2) NOT NULL,
    ActualHours     DECIMAL(5,2) NOT NULL DEFAULT(0),
    PaymentAmount   DECIMAL(12,2) NOT NULL DEFAULT(0),
    CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    RowVersion      ROWVERSION
);
GO

-- HR.tbl_TimeRecoveryPlans
-- Planificación de recuperación de horas/días
IF OBJECT_ID('HR.tbl_TimeRecoveryPlans','U') IS NOT NULL DROP TABLE HR.tbl_TimeRecoveryPlans;
CREATE TABLE HR.tbl_TimeRecoveryPlans(
    RecoveryPlanID  INT IDENTITY(1,1) NOT NULL,
    EmployeeID      INT NOT NULL,
    OwedMinutes     INT NOT NULL,
    PlanDate        DATE NOT NULL,
    FromTime        TIME NOT NULL,
    ToTime          TIME NOT NULL,
    Reason          VARCHAR(300) NULL,
    CreatedBy       INT NULL,
    CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    RowVersion      ROWVERSION
);
GO

-- HR.tbl_TimeRecoveryLogs
-- Log de recuperación
IF OBJECT_ID('HR.tbl_TimeRecoveryLogs','U') IS NOT NULL DROP TABLE HR.tbl_TimeRecoveryLogs;
CREATE TABLE HR.tbl_TimeRecoveryLogs(
    RecoveryLogID   INT IDENTITY(1,1) NOT NULL,
    RecoveryPlanID  INT NOT NULL,
    ExecutedDate    DATE NOT NULL,
    MinutesRecovered INT NOT NULL,
    ApprovedBy      INT NULL,
    ApprovedAt      DATETIME2(0) NULL
);
GO */

IF NOT EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = 'PLAN_STATUS')
BEGIN
    INSERT INTO HR.ref_Types (Category, Name, Description) VALUES
    ('PLAN_STATUS', 'Borrador', 'Planificación en creación, no enviada para aprobación'),
    ('PLAN_STATUS', 'Pendiente', 'Enviada para aprobación, esperando revisión'),
    ('PLAN_STATUS', 'Aprobado', 'Aprobada por todas las instancias, lista para ejecutar'),
    ('PLAN_STATUS', 'Rechazado', 'Rechazada por inconsistencias o no aprobación'),
    ('PLAN_STATUS', 'En Progreso', 'En período de ejecución, empleados trabajando'),
    ('PLAN_STATUS', 'Completado', 'Finalizada exitosamente, todas las horas ejecutadas'),
    ('PLAN_STATUS', 'Cancelado', 'Anulada por cambio de planes o circunstancias');
    
    PRINT 'Estados de planificación general insertados en ref_Types (PLAN_STATUS)';
END
GO

-- HR.tbl_Holidays
-------------------------------------------------------------------------------
-- 3. CREAR TABLAS (SIN CONSTRAINTS FK)
-------------------------------------------------------------------------------

-- 3.1 Tabla de Feriados Simplificada
IF OBJECT_ID('HR.tbl_Holidays','U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_Holidays(
        -- ID único del feriado
        HolidayID INT IDENTITY(1,1) NOT NULL,
        
        -- Nombre descriptivo del feriado (ej: "Navidad", "Año Nuevo")
        Name VARCHAR(100) NOT NULL,
        
        -- Fecha específica del feriado
        HolidayDate DATE NOT NULL,
        
        -- Estado activo/inactivo del feriado (para deshabilitar sin eliminar)
        IsActive BIT NOT NULL DEFAULT(1),
        
        -- Descripción adicional o observaciones
        Description VARCHAR(255) NULL,
        
        -- Fecha de creación del registro
        CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
    );
    PRINT 'Tabla HR.tbl_Holidays creada exitosamente';
END
GO

-- HR.tbl_TimePlanning
-- 3.2 Tabla Unificada de Planificación
IF OBJECT_ID('HR.tbl_TimePlanning','U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_TimePlanning(
        -- ID único de la planificación
        PlanID INT IDENTITY(1,1) NOT NULL,
        
        -- Tipo de planificación: Horas Extras o Recuperación
        PlanType VARCHAR(20) NOT NULL,
        
        -- Título descriptivo de la planificación
        Title VARCHAR(200) NOT NULL,
        
        -- Descripción detallada de la planificación
        Description VARCHAR(500) NULL,
        
        -- Fecha de inicio del período de planificación
        StartDate DATE NOT NULL,
        
        -- Fecha de fin del período de planificación
        EndDate DATE NOT NULL,
        
        -- Hora de inicio diaria para la planificación
        StartTime TIME NOT NULL,
        
        -- Hora de fin diaria para la planificación
        EndTime TIME NOT NULL,
        
        -- Tipo de hora extra (solo para PlanType = 'Overtime')
        OvertimeType VARCHAR(50) NULL,
        
        -- Factor de pago para horas extras (ej: 1.5, 2.0)
        Factor DECIMAL(5,2) NULL,
        
        -- Minutos a recuperar (solo para PlanType = 'Recovery')
        OwedMinutes INT NULL,
        
        -- Estado de la planificación (FK a ref_Types - PLAN_STATUS)
        PlanStatusTypeID INT NOT NULL,
        
        -- Indica si requiere aprobación de superiores
        RequiresApproval BIT NOT NULL DEFAULT(1),
        
        -- Empleado que aprobó la planificación
        ApprovedBy INT NULL,
        
        -- Segundo aprobador para validaciones adicionales
        SecondApprover INT NULL,
        
        -- Fecha y hora de aprobación
        ApprovedAt DATETIME2(0) NULL,
        
        -- Empleado que creó la planificación
        CreatedBy INT NOT NULL,
        
        -- Fecha de creación del registro
        CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
        
        -- Empleado que realizó la última actualización
        UpdatedBy INT NULL,
        
        -- Fecha de última actualización
        UpdatedAt DATETIME2(0) NULL,
        
        -- Control de concurrencia optimista
        RowVersion ROWVERSION
    );
    PRINT 'Tabla HR.tbl_TimePlanning creada exitosamente';
END
GO

-- HR.tbl_TimePlanningEmployees
-- 3.3 Tabla de Empleados en Planificación
IF OBJECT_ID('HR.tbl_TimePlanningEmployees','U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_TimePlanningEmployees(
        -- ID único de la relación empleado-planificación
        PlanEmployeeID INT IDENTITY(1,1) NOT NULL,
        
        -- FK a la planificación principal
        PlanID INT NOT NULL,
        
        -- FK al empleado asignado
        EmployeeID INT NOT NULL,
        
        -- Horas asignadas al empleado (para horas extras)
        AssignedHours DECIMAL(5,2) NULL,
        
        -- Minutos asignados al empleado (para recuperación)
        AssignedMinutes INT NULL,
        
        -- Horas reales trabajadas por el empleado
        ActualHours DECIMAL(5,2) NULL DEFAULT(0),
        
        -- Minutos reales trabajados por el empleado
        ActualMinutes INT NULL DEFAULT(0),
        
        -- Estado individual del empleado en la planificación (FK a ref_Types)
        EmployeeStatusTypeID INT NOT NULL,
        
        -- Monto calculado a pagar al empleado
        PaymentAmount DECIMAL(12,2) NULL DEFAULT(0),
        
        -- Indica si el empleado es elegible para la planificación
        IsEligible BIT NOT NULL DEFAULT(1),
        
        -- Razón por la cual el empleado no es elegible (si aplica)
        EligibilityReason VARCHAR(300) NULL,
        
        -- Fecha de creación del registro
        CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
    );
    PRINT 'Tabla HR.tbl_TimePlanningEmployees creada exitosamente';
END
GO

-- HR.tbl_TimePlanningExecution
-- 3.4 Tabla de Ejecución de Planificación
IF OBJECT_ID('HR.tbl_TimePlanningExecution','U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_TimePlanningExecution(
        -- ID único de la ejecución
        ExecutionID INT IDENTITY(1,1) NOT NULL,
        
        -- FK al empleado en la planificación
        PlanEmployeeID INT NOT NULL,
        
        -- Fecha específica de trabajo
        WorkDate DATE NOT NULL,
        
        -- Hora de inicio real de la jornada
        StartTime DATETIME2(0) NULL,
        
        -- Hora de fin real de la jornada
        EndTime DATETIME2(0) NULL,
        
        -- Total de minutos trabajados
        TotalMinutes INT NOT NULL DEFAULT(0),
        
        -- Minutos en horario regular
        RegularMinutes INT NOT NULL DEFAULT(0),
        
        -- Minutos en horario extra
        OvertimeMinutes INT NOT NULL DEFAULT(0),
        
        -- Minutos en turno nocturno (22:00-06:00)
        NightMinutes INT NOT NULL DEFAULT(0),
        
        -- Minutos trabajados en feriados
        HolidayMinutes INT NOT NULL DEFAULT(0),
        
        -- Empleado que verificó la ejecución
        VerifiedBy INT NULL,
        
        -- Fecha y hora de verificación
        VerifiedAt DATETIME2(0) NULL,
        
        -- Comentarios u observaciones de la verificación
        Comments VARCHAR(500) NULL,
        
        -- Fecha de creación del registro
        CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
    );
    PRINT 'Tabla HR.tbl_TimePlanningExecution creada exitosamente';
END
GO

-- HR.tbl_Subrogations
-- Subrogaciones
IF OBJECT_ID('HR.tbl_Subrogations','U') IS NOT NULL DROP TABLE HR.tbl_Subrogations;
CREATE TABLE HR.tbl_Subrogations(
    SubrogationID           INT IDENTITY(1,1) NOT NULL,
    SubrogatedEmployeeID    INT NOT NULL,
    SubrogatingEmployeeID   INT NOT NULL,
    StartDate               DATE NOT NULL,
    EndDate                 DATE NOT NULL,
    PermissionID            INT NULL,
    VacationID              INT NULL,
    Reason                  VARCHAR(300) NULL,
    CreatedAt               DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    RowVersion              ROWVERSION
);
GO

-- HR.tbl_PersonnelMovements
-- Movimientos de personal
IF OBJECT_ID('HR.tbl_PersonnelMovements','U') IS NOT NULL DROP TABLE HR.tbl_PersonnelMovements;
CREATE TABLE HR.tbl_PersonnelMovements(
    MovementID          INT IDENTITY(1,1) NOT NULL,
    EmployeeID          INT NOT NULL,
    ContractID          INT NOT NULL,
	JobID				INT NOT NULL,
    OriginDepartmentID  INT NULL,
    DestinationDepartmentID INT NOT NULL,
    MovementDate        DATE  NULL,
    MovementType        VARCHAR(30)  NULL CHECK (MovementType IN ('Transfer','Promotion','Demotion','Lateral')),
    DocumentLocation    VARCHAR(255) NULL,
    Reason              VARCHAR(500) NULL,
	IsActive            BIT             NOT NULL DEFAULT(1),
    CreatedBy           INT NULL,
    CreatedAt           DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    RowVersion          ROWVERSION
);
GO

-- HR.tbl_Payroll
-- Nómina
IF OBJECT_ID('HR.tbl_Payroll','U') IS NOT NULL DROP TABLE HR.tbl_Payroll;
CREATE TABLE HR.tbl_Payroll(
    PayrollID       INT IDENTITY(1,1) NOT NULL,
    EmployeeID      INT NOT NULL,
    Period          CHAR(7) NOT NULL, -- YYYY-MM
    BaseSalary      DECIMAL(12,2) NOT NULL,
    Status          VARCHAR(15) NOT NULL DEFAULT('Pending') CHECK (Status IN ('Pending','Paid','Reconciled')),
    PaymentDate     DATE NULL,
    BankAccount     VARCHAR(50) NULL,
    CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,
    RowVersion      ROWVERSION
);
GO

-- HR.tbl_PayrollLines
IF OBJECT_ID('HR.tbl_PayrollLines','U') IS NOT NULL DROP TABLE HR.tbl_PayrollLines;
CREATE TABLE HR.tbl_PayrollLines(
    PayrollLineID   INT IDENTITY(1,1) NOT NULL,
    PayrollID       INT NOT NULL,
    LineType        VARCHAR(20) NOT NULL CHECK (LineType IN ('Earning','Deduction','Subsidy','Overtime')),
    Concept         VARCHAR(120) NOT NULL,
    Quantity        DECIMAL(10,2) NOT NULL DEFAULT(1),
    UnitValue       DECIMAL(12,2) NOT NULL DEFAULT(0),
    Notes           VARCHAR(300) NULL
);
GO

-- HR.tbl_Audit
-- Auditoría
IF OBJECT_ID('HR.tbl_Audit','U') IS NOT NULL DROP TABLE HR.tbl_Audit;
CREATE TABLE HR.tbl_Audit(
    AuditID     BIGINT IDENTITY(1,1) NOT NULL,
    TableName   SYSNAME      NOT NULL,
    Action      VARCHAR(20)  NOT NULL,
    RecordID    NVARCHAR(64) NOT NULL,
    UserName    SYSNAME      NOT NULL DEFAULT SUSER_SNAME(),
    DateTime    DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    Details     NVARCHAR(MAX) NULL
);
GO

-- dbutasystem.HR
IF OBJECT_ID('HR.tbl_jobs','U') IS NOT NULL DROP TABLE HR.tbl_jobs;

CREATE TABLE dbutasystem.HR.tbl_jobs (
    JobID INT PRIMARY KEY IDENTITY(1,1), -- Unique identifier for each job.
    Title VARCHAR(255) NOT NULL, -- The official job title (e.g., 'Software Engineer').
    --Department VARCHAR(100), -- The department where the job belongs (e.g., 'Engineering').
    Description TEXT, -- A detailed description of the job responsibilities and requirements.
    --MinSalary DECIMAL(10, 2), -- The minimum salary for the position.
    --MaxSalary DECIMAL(10, 2), -- The maximum salary for the position.
	IsActive BIT NOT NULL DEFAULT(1),
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE() -- The date and time the job was created.
);
GO
-- =========================================
-- 3) CONSTRAINTS (separate ALTER TABLE ADD CONSTRAINT)
-- =========================================
-- Source: 01_1-ScriptHojaVida.sql
/* =========================================================
   CLAVES PRIMARIAS PARA TODAS LAS TABLAS
   ========================================================= */


-- Tablas geográficas
ALTER TABLE HR.tbl_Countries ADD CONSTRAINT PK_Countries PRIMARY KEY (CountryID);
ALTER TABLE HR.tbl_Provinces ADD CONSTRAINT PK_Provinces PRIMARY KEY (ProvinceID);
ALTER TABLE HR.tbl_Cantons ADD CONSTRAINT PK_Cantons PRIMARY KEY (CantonID);
GO

-- Source: 01_1-ScriptHojaVida.sql
-- Tablas de historia de vida
ALTER TABLE HR.tbl_Addresses ADD CONSTRAINT PK_Addresses PRIMARY KEY (AddressID);
ALTER TABLE HR.tbl_Institutions ADD CONSTRAINT PK_Institutions PRIMARY KEY (InstitutionID);
ALTER TABLE HR.tbl_EducationLevels ADD CONSTRAINT PK_EducationLevels PRIMARY KEY (EducationID);
ALTER TABLE HR.tbl_EmergencyContacts ADD CONSTRAINT PK_EmergencyContacts PRIMARY KEY (ContactID);
ALTER TABLE HR.tbl_CatastrophicIllnesses ADD CONSTRAINT PK_CatastrophicIllnesses PRIMARY KEY (IllnessID);
ALTER TABLE HR.tbl_FamilyBurden ADD CONSTRAINT PK_FamilyBurden PRIMARY KEY (BurdenID);
ALTER TABLE HR.tbl_Trainings ADD CONSTRAINT PK_Trainings PRIMARY KEY (TrainingID);
ALTER TABLE HR.tbl_WorkExperiences ADD CONSTRAINT PK_WorkExperiences PRIMARY KEY (WorkExpID);
ALTER TABLE HR.tbl_BankAccounts ADD CONSTRAINT PK_BankAccounts PRIMARY KEY (AccountID);
ALTER TABLE HR.tbl_Publications ADD CONSTRAINT PK_Publications PRIMARY KEY (PublicationID);
ALTER TABLE HR.tbl_Books ADD CONSTRAINT PK_Books PRIMARY KEY (BookID);
GO

-- Source: 01_1-ScriptHojaVida.sql
/* =========================================================
   RESTRICCIONES UNICAS
   ========================================================= */
ALTER TABLE HR.ref_Types ADD CONSTRAINT UQ_TypeCategoryName UNIQUE (Category, Name);
-- Removidas las restricciones únicas de CountryCode, ProvinceCode, CantonCode ya que estos campos no existen
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 4. AGREGAR PRIMARY KEYS
-------------------------------------------------------------------------------

-- 4.1 Primary Key para tbl_Holidays
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Holidays')
    ALTER TABLE HR.tbl_Holidays ADD CONSTRAINT PK_Holidays PRIMARY KEY (HolidayID);

-- 4.2 Primary Key para tbl_TimePlanning
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanning')
    ALTER TABLE HR.tbl_TimePlanning ADD CONSTRAINT PK_TimePlanning PRIMARY KEY (PlanID);

-- 4.3 Primary Key para tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanningEmployees')
    ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT PK_TimePlanningEmployees PRIMARY KEY (PlanEmployeeID);

-- 4.4 Primary Key para tbl_TimePlanningExecution
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanningExecution')
    ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT PK_TimePlanningExecution PRIMARY KEY (ExecutionID);

PRINT 'Primary Keys agregados exitosamente';
GO
-- =========================================
-- 4) INDEXES
-- =========================================
-- Source: 01_1-ScriptHojaVida.sql
/* =========================================================
   ÍNDICES PARA OPTIMIZACIÓN
   ========================================================= */
-- Personas
CREATE INDEX IX_People_IDCard ON HR.tbl_People(IDCard);
CREATE INDEX IX_People_LastName ON HR.tbl_People(LastName);

-- Direcciones
CREATE INDEX IX_Addresses_Person ON HR.tbl_Addresses(PersonID);

-- Educación
CREATE INDEX IX_Education_Person ON HR.tbl_EducationLevels(PersonID);

-- Experiencia Laboral
CREATE INDEX IX_WorkExp_Person ON HR.tbl_WorkExperiences(PersonID);
CREATE INDEX IX_WorkExp_IsCurrent ON HR.tbl_WorkExperiences(IsCurrent);

-- Libros y Publicaciones
CREATE INDEX IX_Books_Person ON HR.tbl_Books(PersonID);
CREATE INDEX IX_Publications_Person ON HR.tbl_Publications(PersonID);

-- Índices geográficos
CREATE INDEX IX_Provinces_Country ON HR.tbl_Provinces(CountryID);
CREATE INDEX IX_Cantons_Province ON HR.tbl_Cantons(ProvinceID);
CREATE INDEX IX_Addresses_Geographic ON HR.tbl_Addresses(CountryID, ProvinceID, CantonID);
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 5. AGREGAR CONSTRAINTS CHECK
-------------------------------------------------------------------------------

-- 5.1 Check constraints para tbl_TimePlanning
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanning_PlanType')
    ALTER TABLE HR.tbl_TimePlanning ADD CONSTRAINT CK_TimePlanning_PlanType 
    CHECK (PlanType IN ('Overtime','Recovery'));

-- 5.2 Check constraints para tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanningEmployees_Hours')
    ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT CK_TimePlanningEmployees_Hours 
    CHECK (AssignedHours >= 0 OR AssignedHours IS NULL);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanningEmployees_Minutes')
    ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT CK_TimePlanningEmployees_Minutes 
    CHECK (AssignedMinutes >= 0 OR AssignedMinutes IS NULL);

-- 5.3 Unique constraint para feriados (misma fecha no puede repetirse)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Holidays_Date' AND object_id = OBJECT_ID('HR.tbl_Holidays'))
    CREATE UNIQUE INDEX UX_Holidays_Date ON HR.tbl_Holidays(HolidayDate) WHERE IsActive = 1;

PRINT 'Check constraints agregados exitosamente';
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 6. CREAR ÍNDICES
-------------------------------------------------------------------------------

-- 6.1 Índices para tbl_TimePlanning
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanning_Dates' AND object_id = OBJECT_ID('HR.tbl_TimePlanning'))
    CREATE INDEX IX_TimePlanning_Dates ON HR.tbl_TimePlanning(StartDate, EndDate);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanning_Status' AND object_id = OBJECT_ID('HR.tbl_TimePlanning'))
    CREATE INDEX IX_TimePlanning_Status ON HR.tbl_TimePlanning(PlanStatusTypeID);

-- 6.2 Índices para tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningEmployees_Employee' AND object_id = OBJECT_ID('HR.tbl_TimePlanningEmployees'))
    CREATE INDEX IX_TimePlanningEmployees_Employee ON HR.tbl_TimePlanningEmployees(EmployeeID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningEmployees_Status' AND object_id = OBJECT_ID('HR.tbl_TimePlanningEmployees'))
    CREATE INDEX IX_TimePlanningEmployees_Status ON HR.tbl_TimePlanningEmployees(EmployeeStatusTypeID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningEmployees_Plan' AND object_id = OBJECT_ID('HR.tbl_TimePlanningEmployees'))
    CREATE INDEX IX_TimePlanningEmployees_Plan ON HR.tbl_TimePlanningEmployees(PlanID);

-- 6.3 Índices para tbl_TimePlanningExecution
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningExecution_Date' AND object_id = OBJECT_ID('HR.tbl_TimePlanningExecution'))
    CREATE INDEX IX_TimePlanningExecution_Date ON HR.tbl_TimePlanningExecution(WorkDate, PlanEmployeeID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningExecution_PlanEmployee' AND object_id = OBJECT_ID('HR.tbl_TimePlanningExecution'))
    CREATE INDEX IX_TimePlanningExecution_PlanEmployee ON HR.tbl_TimePlanningExecution(PlanEmployeeID);

-- 6.4 Índices para tbl_Holidays
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Holidays_Date' AND object_id = OBJECT_ID('HR.tbl_Holidays'))
    CREATE INDEX IX_Holidays_Date ON HR.tbl_Holidays(HolidayDate) WHERE IsActive = 1;

PRINT 'Índices creados exitosamente';
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 3) ÍNDICES
-------------------------------------------------------------------------------
CREATE INDEX IX_Employees_Department       ON HR.tbl_Employees(DepartmentID);
CREATE INDEX IX_Employees_Boss             ON HR.tbl_Employees(ImmediateBossID);
CREATE INDEX IX_Punch_EmployeeDate         ON HR.tbl_AttendancePunches(EmployeeID, CONVERT(date, PunchTime));
CREATE INDEX IX_OT_EmployeeDate            ON HR.tbl_Overtime(EmployeeID, WorkDate);
CREATE INDEX IX_Vacations_EmployeeDate     ON HR.tbl_Vacations(EmployeeID, StartDate, EndDate);
CREATE INDEX IX_Permissions_EmployeeDate   ON HR.tbl_Permissions(EmployeeID, StartDate, EndDate);

CREATE UNIQUE INDEX UX_hr_employees_Email ON hr.tbl_employees(Email);

-- Índices adicionales para mejorar performance
CREATE NONCLUSTERED INDEX [IX_PunchJustifications_Employee] ON [HR].[tbl_PunchJustifications]
(
	[EmployeeID] ASC,
	[Status] ASC
)
INCLUDE ([JustificationTypeID], [JustificationDate], [CreatedAt])
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE NONCLUSTERED INDEX [IX_PunchJustifications_Boss] ON [HR].[tbl_PunchJustifications]
(
	[BossEmployeeID] ASC,
	[Status] ASC
)
GO
-- =========================================
-- 5) TRIGGERS
-- =========================================
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
-- =========================================
-- 6) FUNCTIONS
-- =========================================
-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE FUNCTION HR.fn_GetBusinessDays(@StartDate date, @EndDate date)
RETURNS INT
AS
BEGIN
    DECLARE @d date = @StartDate, @cnt int = 0;
    WHILE @d <= @EndDate
    BEGIN
        IF DATENAME(WEEKDAY,@d) NOT IN ('Saturday','Sunday') SET @cnt += 1;
        SET @d = DATEADD(DAY,1,@d);
    END
    RETURN @cnt;
END
GO
-- =========================================
-- 7) PROCEDURES
-- =========================================
-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE PROCEDURE HR.sp_RequestVacation
    @EmployeeID INT,
    @StartDate  DATE,
    @EndDate    DATE,
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @days INT = DATEDIFF(DAY,@StartDate,@EndDate) + 1;

    DECLARE @avail INT = ISNULL((SELECT TOP 1 DaysGranted - DaysTaken FROM HR.tbl_Vacations WHERE EmployeeID=@EmployeeID ORDER BY VacationID DESC),0);
    IF @avail < @days
        RAISERROR('Días de vacaciones insuficientes.',16,1);

    INSERT INTO HR.tbl_Vacations(EmployeeID,StartDate,EndDate,DaysGranted,Status)
    VALUES(@EmployeeID,@StartDate,@EndDate,@days,'Planned');

    DECLARE @vacationID INT = SCOPE_IDENTITY();

    INSERT INTO HR.tbl_Permissions(EmployeeID,PermissionTypeID,StartDate,EndDate,ApprovedBy,Status,VacationID)
    VALUES(@EmployeeID,(SELECT TOP 1 TypeID FROM HR.tbl_PermissionTypes WHERE Name='Vacaciones'),
           @StartDate,@EndDate,@ApproverID,'Pending',@vacationID);

    SELECT @vacationID AS VacationID;
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE PROCEDURE HR.sp_PlanOvertime
    @EmployeeID INT,
    @Date DATE,
    @Hours DECIMAL(5,2),
    @OvertimeType VARCHAR(50),
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Factor DECIMAL(5,2) = ISNULL((SELECT Factor FROM HR.tbl_OvertimeConfig WHERE OvertimeType=@OvertimeType),1.25);
    INSERT INTO HR.tbl_Overtime(EmployeeID,WorkDate,OvertimeType,Hours,Status,ApprovedBy,Factor)
    VALUES(@EmployeeID,@Date,@OvertimeType,@Hours,'Planned',@ApproverID,@Factor);
    SELECT SCOPE_IDENTITY() AS OvertimeID;
END
GO */

-------------------------------------------------------------------------------
-- 5) INSERTS DE PRUEBA
-------------------------------------------------------------------------------

/* =========================================================
   INSERCIÓN DE DATOS INICIALES EN TABLA DE TIPOS
   ========================================================= */
INSERT INTO HR.ref_Types (Category, Name) 
VALUES 
    ('MARITAL_STATUS', 'Soltero/a'),
    ('MARITAL_STATUS', 'Casado/a'),
    ('MARITAL_STATUS', 'Divorciado/a'),
    ('MARITAL_STATUS', 'Viudo/a'),
    ('MARITAL_STATUS', 'Unión libre'),
    ('ETHNICITY', 'Mestizo'),
    ('ETHNICITY', 'Afroecuatoriano'),
    ('ETHNICITY', 'Indígena'),
    ('ETHNICITY', 'Montubio'),
    ('ETHNICITY', 'Blanco'),
    ('ETHNICITY', 'Otro'),
    ('BLOOD_TYPE', 'A+'),
    ('BLOOD_TYPE', 'A-'),
    ('BLOOD_TYPE', 'B+'),
    ('BLOOD_TYPE', 'B-'),
    ('BLOOD_TYPE', 'AB+'),
    ('BLOOD_TYPE', 'AB-'),
    ('BLOOD_TYPE', 'O+'),
    ('BLOOD_TYPE', 'O-'),
    ('SPECIAL_NEEDS', 'Trastorno emocional'),
    ('SPECIAL_NEEDS', 'Trastorno de personalidad'),
    ('SPECIAL_NEEDS', 'Problemas de aprendizaje'),
    ('SPECIAL_NEEDS', 'Otras necesidades especiales'),
    ('DISABILITY_TYPE', 'Física'),
    ('DISABILITY_TYPE', 'Mental'),
    ('DISABILITY_TYPE', 'Auditiva'),
    ('DISABILITY_TYPE', 'Visual'),
    ('DISABILITY_TYPE', 'Otra'),
    ('RELATIONSHIP', 'Cónyuge'),
    ('RELATIONSHIP', 'Hijo/a'),
    ('RELATIONSHIP', 'Padre'),
    ('RELATIONSHIP', 'Madre'),
    ('RELATIONSHIP', 'Hermano/a'),
    ('RELATIONSHIP', 'Abuelo/a'),
    ('RELATIONSHIP', 'Tío/a'),
    ('RELATIONSHIP', 'Primo/a'),
    ('RELATIONSHIP', 'Otro'),
    ('INSTITUTION_TYPE', 'Educativa'),
    ('INSTITUTION_TYPE', 'Gubernamental'),
    ('INSTITUTION_TYPE', 'Privada'),
    ('INSTITUTION_TYPE', 'ONG'),
    ('INSTITUTION_TYPE', 'Religiosa'),
    ('INSTITUTION_TYPE', 'Otro'),
    ('EVENT_TYPE', 'Charla'),
    ('EVENT_TYPE', 'Conferencia'),
    ('EVENT_TYPE', 'Taller'),
    ('EVENT_TYPE', 'Seminario'),
    ('EVENT_TYPE', 'Diplomado'),
    ('EVENT_TYPE', 'Curso'),
    ('EVENT_TYPE', 'Otro'),
    ('KNOWLEDGE_AREA', 'Tecnológico'),
    ('KNOWLEDGE_AREA', 'Educativo'),
    ('KNOWLEDGE_AREA', 'Salud'),
    ('KNOWLEDGE_AREA', 'Administrativo'),
    ('KNOWLEDGE_AREA', 'Humanístico'),
    ('KNOWLEDGE_AREA', 'Otro'),
    ('ADDRESS_TYPE', 'Domicilio'),
    ('ADDRESS_TYPE', 'Trabajo'),
    ('ADDRESS_TYPE', 'Temporal'),
    ('ACCOUNT_TYPE', 'Ahorros'),
    ('ACCOUNT_TYPE', 'Corriente'),
    ('EDUCATION_LEVEL', 'Primaria'),
    ('EDUCATION_LEVEL', 'Secundaria'),
    ('EDUCATION_LEVEL', 'Técnico'),
    ('EDUCATION_LEVEL', 'Tecnológico'),
    ('EDUCATION_LEVEL', 'Tercer Nivel'),
    ('EDUCATION_LEVEL', 'Cuarto Nivel'),
    ('PUBLICATION_TYPE', 'Artículo científico'),
    ('PUBLICATION_TYPE', 'Artículo de revisión'),
    ('PUBLICATION_TYPE', 'Ensayo'),
    ('JOURNAL_TYPE', 'Internacional'),
    ('JOURNAL_TYPE', 'Nacional'),
    ('JOURNAL_TYPE', 'Regional'),
    ('CERTIFICATE_TYPE', 'Físico'),
    ('CERTIFICATE_TYPE', 'Digital'),
    ('APPROVAL_TYPE', 'Aprobado'),
    ('APPROVAL_TYPE', 'Reprobado'),
    ('EXPERIENCE_TYPE', 'Público'),
    ('EXPERIENCE_TYPE', 'Privado'),
    ('EXPERIENCE_TYPE', 'ONG'),
    ('ILLNESS_TYPE', 'Cáncer'),
    ('ILLNESS_TYPE', 'Diabetes'),
    ('ILLNESS_TYPE', 'Hipertensión'),
    ('ILLNESS_TYPE', 'Cardiopatía'),
    ('ILLNESS_TYPE', 'Otra'),
    ('IDENTIFICATION_TYPE', 'Cédula'),
    ('IDENTIFICATION_TYPE', 'Pasaporte'),
    ('IDENTIFICATION_TYPE', 'RUC');
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_CalculateDailyAttendance
    @EmployeeID INT,
    @WorkDate   DATE
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
              AND CAST(PunchTime AS date) = @WorkDate
        ),
        Paired AS (
            SELECT
                i.PunchTime AS InTime,
                o.PunchTime AS OutTime
            FROM Punches i
            LEFT JOIN Punches o
              ON o.RN = i.RN + 1
            WHERE i.PunchType = 'In' AND o.PunchType = 'Out'
        )
        SELECT
            @EmployeeID AS EmployeeID,
            @WorkDate   AS WorkDate,
            MIN(InTime)  AS FirstPunchIn,
            MAX(OutTime) AS LastPunchOut,
            SUM(CASE WHEN OutTime IS NOT NULL THEN DATEDIFF(MINUTE, InTime, OutTime) ELSE 0 END) AS TotalMinutes
        INTO #Agg
        FROM Paired;

        DECLARE @first DATETIME2, @last DATETIME2, @total INT;
        SELECT @first = FirstPunchIn, @last = LastPunchOut, @total = ISNULL(TotalMinutes,0) FROM #Agg;

        DECLARE @regular INT = IIF(@total > 480, 480, @total);
        DECLARE @overtime INT = IIF(@total > 480, @total - 480, 0);

        MERGE HR.tbl_AttendanceCalculations AS T
        USING (SELECT @EmployeeID AS EmployeeID, @WorkDate AS WorkDate) AS S
        ON (T.EmployeeID = S.EmployeeID AND T.WorkDate = S.WorkDate)
        WHEN MATCHED THEN UPDATE SET
            FirstPunchIn = @first,
            LastPunchOut = @last,
            TotalWorkedMinutes = @total,
            RegularMinutes = @regular,
            OvertimeMinutes = @overtime,
            Status = 'Approved'
        WHEN NOT MATCHED THEN INSERT (EmployeeID, WorkDate, FirstPunchIn, LastPunchOut, TotalWorkedMinutes, RegularMinutes, OvertimeMinutes, NightMinutes, HolidayMinutes, Status)
        VALUES (@EmployeeID, @WorkDate, @first, @last, @total, @regular, @overtime, 0, 0, 'Approved');

        COMMIT TRANSACTION;
        SELECT 1 AS Ok, 'Asistencia diaria calculada' AS Msg, @total AS TotalMinutes, @overtime AS OvertimeMinutes;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_CalculateMonthlyPayroll
    @EmployeeID INT,
    @Period CHAR(7) -- 'YYYY-MM'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @first DATE = CONVERT(date, @Period + '-01');
        DECLARE @last  DATE = EOMONTH(@first);

        DECLARE @salary DECIMAL(12,2) =
        (
            SELECT TOP 1 BaseSalary
            FROM HR.tbl_Contracts
            WHERE EmployeeID = @EmployeeID
              AND @first BETWEEN StartDate AND ISNULL(EndDate,'9999-12-31')
            ORDER BY StartDate DESC
        );

        IF NOT EXISTS (SELECT 1 FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period)
        BEGIN
            INSERT INTO HR.tbl_Payroll(EmployeeID, Period, BaseSalary, Status)
            VALUES(@EmployeeID, @Period, ISNULL(@salary,0), 'Pending');
        END
        ELSE
        BEGIN
            UPDATE HR.tbl_Payroll SET BaseSalary = ISNULL(@salary, BaseSalary) WHERE EmployeeID=@EmployeeID AND Period=@Period;
        END

        DECLARE @payrollID INT = (SELECT PayrollID FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period);

        ;WITH OT AS (
            SELECT WorkDate, OvertimeType, SUM(ActualHours) AS Hours, MAX(Factor) AS Factor
            FROM HR.tbl_Overtime
            WHERE EmployeeID=@EmployeeID AND Status='Verified' AND WorkDate BETWEEN @first AND @last
            GROUP BY WorkDate, OvertimeType
        )
        INSERT INTO HR.tbl_PayrollLines(PayrollID, LineType, Concept, Quantity, UnitValue, Notes)
        SELECT @payrollID, 'Overtime',
               CONCAT('Horas extra ', OvertimeType, ' ', CONVERT(char(10), WorkDate, 23)) AS Concept,
               Hours, 
               ROUND(ISNULL(@salary,0) / 240.0 * Factor, 2), -- valor hora * factor
               NULL
        FROM OT o
        WHERE NOT EXISTS (
            SELECT 1 FROM HR.tbl_PayrollLines pl
            WHERE pl.PayrollID=@payrollID AND pl.LineType='Overtime' AND pl.Concept=CONCAT('Horas extra ', o.OvertimeType, ' ', CONVERT(char(10), o.WorkDate, 23))
        );

        COMMIT TRANSACTION;
        SELECT 1 AS Ok, 'Nómina calculada/actualizada' AS Msg, @payrollID AS PayrollID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_GenerateMonthlyAttendanceReport
    @Period CHAR(7),             -- 'YYYY-MM'
    @DepartmentID INT = NULL     -- NULL = todos
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @first DATE = CONVERT(date, @Period + '-01');
    DECLARE @last  DATE = EOMONTH(@first);

    SELECT 
        e.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        d.Name AS Department,
        ac.WorkDate,
        ac.TotalWorkedMinutes,
        ac.RegularMinutes,
        ac.OvertimeMinutes,
        ac.NightMinutes,
        ac.HolidayMinutes,
        ac.Status
    FROM HR.tbl_AttendanceCalculations ac
    JOIN HR.tbl_Employees e ON e.EmployeeID = ac.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
    WHERE ac.WorkDate BETWEEN @first AND @last
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
    ORDER BY Department, EmployeeName, ac.WorkDate;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_RegisterPersonnelMovement
    @EmployeeID INT,
    @ContractID INT,
    @OriginDepartmentID INT = NULL,
    @DestinationDepartmentID INT,
    @MovementDate DATE,
    @MovementType VARCHAR(30),
    @Reason VARCHAR(500) = NULL,
    @DocumentLocation VARCHAR(255) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT INTO HR.tbl_PersonnelMovements(EmployeeID, ContractID, OriginDepartmentID, DestinationDepartmentID, MovementDate, MovementType, DocumentLocation, Reason, CreatedBy)
        VALUES(@EmployeeID, @ContractID, @OriginDepartmentID, @DestinationDepartmentID, @MovementDate, @MovementType, @DocumentLocation, @Reason, @CreatedBy);

        -- actualizar depto actual
        UPDATE HR.tbl_Employees SET DepartmentID = @DestinationDepartmentID, UpdatedBy = @CreatedBy, UpdatedAt = SYSDATETIME()
        WHERE EmployeeID = @EmployeeID;

        COMMIT TRANSACTION;
        SELECT SCOPE_IDENTITY() AS MovementID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_AssignEmployeeSubsidy
    @EmployeeID INT,
    @Period CHAR(7),
    @Concept VARCHAR(120),
    @Amount DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period)
        BEGIN
            DECLARE @salary DECIMAL(12,2) = ISNULL((SELECT TOP 1 BaseSalary FROM HR.tbl_Contracts WHERE EmployeeID=@EmployeeID ORDER BY StartDate DESC),0);
            INSERT INTO HR.tbl_Payroll(EmployeeID, Period, BaseSalary) VALUES(@EmployeeID, @Period, @salary);
        END

        DECLARE @payrollID INT = (SELECT PayrollID FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period);
        IF NOT EXISTS (SELECT 1 FROM HR.tbl_PayrollLines WHERE PayrollID=@payrollID AND LineType='Subsidy' AND Concept=@Concept)
        BEGIN
            INSERT INTO HR.tbl_PayrollLines(PayrollID, LineType, Concept, Quantity, UnitValue, Notes)
            VALUES(@payrollID, 'Subsidy', @Concept, 1, @Amount, NULL);
        END
        COMMIT TRANSACTION;
        SELECT 1 AS Ok, 'Subsidio asignado' AS Msg;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_ApproveNightOvertime
    @OvertimeID INT,
    @FirstApprover INT,
    @SecondApprover INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Overtime
    SET ApprovedBy = @FirstApprover,
        SecondApprover = @SecondApprover,
        Status = 'Verified'
    WHERE OvertimeID = @OvertimeID AND OvertimeType = 'Nocturna';
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_RequestVacation
    @EmployeeID INT,
    @StartDate  DATE,
    @EndDate    DATE,
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @days INT = DATEDIFF(DAY,@StartDate,@EndDate) + 1;
    DECLARE @avail INT = ISNULL((SELECT TOP 1 DaysGranted - DaysTaken FROM HR.tbl_Vacations WHERE EmployeeID=@EmployeeID ORDER BY VacationID DESC),0);
    IF @avail < @days
        RAISERROR('Días de vacaciones insuficientes.',16,1);

    INSERT INTO HR.tbl_Vacations(EmployeeID,StartDate,EndDate,DaysGranted,Status)
    VALUES(@EmployeeID,@StartDate,@EndDate,@days,'Planned');

    DECLARE @vacationID INT = SCOPE_IDENTITY();
    INSERT INTO HR.tbl_Permissions(EmployeeID,PermissionTypeID,StartDate,EndDate,ApprovedBy,Status,VacationID)
    VALUES(@EmployeeID,(SELECT TOP 1 TypeID FROM HR.tbl_PermissionTypes WHERE Name='Vacaciones'),
           @StartDate,@EndDate,@ApproverID,'Pending',@vacationID);

    SELECT @vacationID AS VacationID;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_ApprovePermission
    @PermissionID INT,
    @ApproverID INT,
    @Approve BIT,
    @Reason NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Permissions
    SET Status = CASE WHEN @Approve = 1 THEN 'Approved' ELSE 'Rejected' END,
        ApprovedBy = @ApproverID,
        Justification = COALESCE(Justification,'') + ISNULL(' | ' + @Reason,'')
    WHERE PermissionID = @PermissionID;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_CalculateSubsidies
    @Period CHAR(7),
    @DepartmentID INT = NULL,
    @Concept VARCHAR(120) = 'Transporte',
    @Amount DECIMAL(12,2) = 30.00
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @first DATE = CONVERT(date, @Period + '-01');
        ;WITH Emp AS (
            SELECT e.EmployeeID
            FROM HR.tbl_Employees e
            WHERE e.IsActive = 1 AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
        )
        SELECT * INTO #Emp FROM Emp;

        DECLARE cur CURSOR LOCAL FOR SELECT EmployeeID FROM #Emp;
        DECLARE @eid INT;
        OPEN cur; FETCH NEXT FROM cur INTO @eid;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC HR.sp_AssignEmployeeSubsidy @EmployeeID=@eid, @Period=@Period, @Concept=@Concept, @Amount=@Amount;
            FETCH NEXT FROM cur INTO @eid;
        END
        CLOSE cur; DEALLOCATE cur;

        COMMIT TRANSACTION;
        SELECT 1 AS Ok, 'Subsidios calculados/asignados' AS Msg;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_PlanOvertime
    @EmployeeID INT,
    @WorkDate DATE,
    @Hours DECIMAL(5,2),
    @OvertimeType VARCHAR(50),
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Factor DECIMAL(5,2) = ISNULL((SELECT Factor FROM HR.tbl_OvertimeConfig WHERE OvertimeType=@OvertimeType),1.25);
    INSERT INTO HR.tbl_Overtime(EmployeeID, WorkDate, OvertimeType, Hours, Status, ApprovedBy, Factor)
    VALUES(@EmployeeID, @WorkDate, @OvertimeType, @Hours, 'Planned', @ApproverID, @Factor);
    SELECT SCOPE_IDENTITY() AS OvertimeID;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_GenerateSubsidyComplianceReport
    @Period CHAR(7),
    @DepartmentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        d.Name AS Department,
        SUM(CASE WHEN pl.LineType='Subsidy' THEN pl.Quantity * pl.UnitValue ELSE 0 END) AS TotalSubsidies
    FROM HR.tbl_Payroll py
    JOIN HR.tbl_PayrollLines pl ON pl.PayrollID = py.PayrollID
    JOIN HR.tbl_Employees e ON e.EmployeeID = py.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
    WHERE py.Period = @Period
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
    GROUP BY e.EmployeeID, p.FirstName, p.LastName, d.Name
    ORDER BY Department, EmployeeName;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_UpsertEmployeeSchedule
    @EmployeeID INT,
    @ScheduleID INT,
    @ValidFrom  DATE,
    @ValidTo    DATE = NULL           -- NULL = vigente
AS
BEGIN
    SET NOCOUNT ON;
    IF @ValidTo IS NOT NULL AND @ValidTo < @ValidFrom
        RAISERROR('ValidTo no puede ser menor a ValidFrom',16,1);

    -- chequear solapamiento
    IF EXISTS(
        SELECT 1 FROM HR.tbl_EmployeeSchedules es
        WHERE es.EmployeeID = @EmployeeID
          AND (@ValidTo IS NULL OR es.ValidFrom <= @ValidTo)
          AND (es.ValidTo IS NULL OR es.ValidTo >= @ValidFrom)
    )
    BEGIN
        RAISERROR('Existe una asignación de horario que se solapa en el rango indicado.',16,1);
        RETURN;
    END

    INSERT INTO HR.tbl_EmployeeSchedules(EmployeeID, ScheduleID, ValidFrom, ValidTo)
    VALUES(@EmployeeID, @ScheduleID, @ValidFrom, @ValidTo);
    SELECT SCOPE_IDENTITY() AS EmpScheduleID;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_GetActiveSchedule
    @EmployeeID INT,
    @ForDate    DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 s.*
    FROM HR.tbl_EmployeeSchedules es
    JOIN HR.tbl_Schedules s ON s.ScheduleID = es.ScheduleID
    WHERE es.EmployeeID = @EmployeeID
      AND @ForDate BETWEEN es.ValidFrom AND ISNULL(es.ValidTo,'9999-12-31')
    ORDER BY es.ValidFrom DESC;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_PlanTimeRecovery
    @EmployeeID INT,
    @OwedMinutes INT,
    @PlanDate DATE,
    @FromTime TIME,
    @ToTime TIME,
    @Reason VARCHAR(300) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF DATEDIFF(MINUTE, @FromTime, @ToTime) < @OwedMinutes OR DATEDIFF(MINUTE, @FromTime, @ToTime) < 60
        RAISERROR('El bloque planificado debe cubrir al menos los minutos adeudados y ser >= 60 minutos.',16,1);

    -- Evitar plan en vacaciones
    IF EXISTS (
        SELECT 1 FROM HR.tbl_Vacations v
        WHERE v.EmployeeID = @EmployeeID
          AND v.Status IN ('Planned','InProgress')
          AND @PlanDate BETWEEN v.StartDate AND v.EndDate
    )
    BEGIN
        RAISERROR('No se puede planificar recuperación durante vacaciones.',16,1);
        RETURN;
    END

    INSERT INTO HR.tbl_TimeRecoveryPlans(EmployeeID,OwedMinutes,PlanDate,FromTime,ToTime,Reason,CreatedBy)
    VALUES(@EmployeeID,@OwedMinutes,@PlanDate,@FromTime,@ToTime,@Reason,@CreatedBy);

    SELECT SCOPE_IDENTITY() AS RecoveryPlanID;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ApproveTimeRecovery
    @RecoveryPlanID INT,
    @ApproverID INT
AS
BEGIN
    SET NOCOUNT ON;
    -- (opcional: agregar columna de estado; por ahora, registramos en logs con 0 minutos como "aprobación")
    INSERT INTO HR.tbl_TimeRecoveryLogs(RecoveryPlanID, ExecutedDate, MinutesRecovered, ApprovedBy, ApprovedAt)
    VALUES(@RecoveryPlanID, CAST(SYSDATETIME() AS DATE), 0, @ApproverID, SYSDATETIME());
    SELECT 1 AS Ok, 'Plan de recuperación aprobado (log registrado)' AS Msg;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_LogTimeRecovery
    @RecoveryPlanID INT,
    @ExecutedDate DATE,
    @MinutesRecovered INT,
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO HR.tbl_TimeRecoveryLogs(RecoveryPlanID, ExecutedDate, MinutesRecovered, ApprovedBy, ApprovedAt)
    VALUES(@RecoveryPlanID, @ExecutedDate, @MinutesRecovered, @ApproverID, CASE WHEN @ApproverID IS NULL THEN NULL ELSE SYSDATETIME() END);
    SELECT SCOPE_IDENTITY() AS RecoveryLogID;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_GetTimeRecoveryBalance
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        @EmployeeID AS EmployeeID,
        ISNULL(SUM(p.OwedMinutes),0) AS MinutesOwed,
        ISNULL((SELECT SUM(l.MinutesRecovered) FROM HR.tbl_TimeRecoveryLogs l JOIN HR.tbl_TimeRecoveryPlans p2 ON p2.RecoveryPlanID=l.RecoveryPlanID WHERE p2.EmployeeID=@EmployeeID),0) AS MinutesRecovered,
        ISNULL(SUM(p.OwedMinutes),0) - ISNULL((SELECT SUM(l.MinutesRecovered) FROM HR.tbl_TimeRecoveryLogs l JOIN HR.tbl_TimeRecoveryPlans p2 ON p2.RecoveryPlanID=l.RecoveryPlanID WHERE p2.EmployeeID=@EmployeeID),0) AS BalanceMinutes
    FROM HR.tbl_TimeRecoveryPlans p
    WHERE p.EmployeeID = @EmployeeID;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_JustifyPunch
    @PunchID INT,
    @BossEmployeeID INT,
    @Reason VARCHAR(500),
    @Approve BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM HR.tbl_PunchJustifications WHERE PunchID=@PunchID)
        UPDATE HR.tbl_PunchJustifications
           SET Reason=@Reason, BossEmployeeID=@BossEmployeeID, Approved=@Approve, ApprovedAt=CASE WHEN @Approve=1 THEN SYSDATETIME() ELSE NULL END
         WHERE PunchID=@PunchID;
    ELSE
        INSERT INTO HR.tbl_PunchJustifications(PunchID,BossEmployeeID,Reason,Approved,ApprovedAt)
        VALUES(@PunchID,@BossEmployeeID,@Reason,@Approve,CASE WHEN @Approve=1 THEN SYSDATETIME() ELSE NULL END);

    SELECT 1 AS Ok, 'Justificación registrada' AS Msg;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_RecalculateAttendanceRange
    @EmployeeID INT,
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF @ToDate < @FromDate RAISERROR('Rango de fechas inválido.',16,1);

    DECLARE @d DATE = @FromDate;
    WHILE @d <= @ToDate
    BEGIN
        EXEC HR.sp_CalculateDailyAttendance @EmployeeID=@EmployeeID, @WorkDate=@d;
        SET @d = DATEADD(DAY, 1, @d);
    END

    SELECT 1 AS Ok, 'Asistencia recalculada para el rango' AS Msg;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_BulkImportPunches
    @Rows HR.tt_PunchImport READONLY
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO HR.tbl_AttendancePunches(EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude)
    SELECT EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude
    FROM @Rows;  -- el trigger HR.trg_Punch_Validations aplicará reglas
    SELECT @@ROWCOUNT AS Imported;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ValidateSubrogationWindow
    @SubrogatedEmployeeID INT,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.SubrogationID, s.StartDate, s.EndDate
    FROM HR.tbl_Subrogations s
    WHERE s.SubrogatedEmployeeID = @SubrogatedEmployeeID
      AND (@StartDate <= s.EndDate AND @EndDate >= s.StartDate);
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_EmployeesOnLeave
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.EmployeeID, p.FirstName + ' ' + p.LastName AS EmployeeName, v.StartDate, v.EndDate, v.Status
    FROM HR.tbl_Vacations v
    JOIN HR.tbl_Employees e ON e.EmployeeID = v.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    WHERE v.StartDate <= @ToDate AND v.EndDate >= @FromDate;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_EmployeesAboutToVacation
    @WithinDays INT = 15
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @today DATE = CAST(SYSDATETIME() AS DATE);
    DECLARE @limit DATE = DATEADD(DAY, @WithinDays, @today);

    SELECT e.EmployeeID, p.FirstName + ' ' + p.LastName AS EmployeeName, v.StartDate, v.EndDate, v.Status
    FROM HR.tbl_Vacations v
    JOIN HR.tbl_Employees e ON e.EmployeeID = v.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    WHERE v.StartDate BETWEEN @today AND @limit
      AND v.Status IN ('Planned','InProgress');
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ListPendingPermissions
    @DepartmentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pr.PermissionID, e.EmployeeID, p.FirstName + ' ' + p.LastName AS EmployeeName,
           d.Name AS Department, t.Name AS PermissionType, pr.StartDate, pr.EndDate, pr.RequestDate
    FROM HR.tbl_Permissions pr
    JOIN HR.tbl_PermissionTypes t ON t.TypeID = pr.PermissionTypeID
    JOIN HR.tbl_Employees e ON e.EmployeeID = pr.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
    WHERE pr.Status = 'Pending'
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
    ORDER BY pr.RequestDate DESC;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_GetPayrollBreakdown
    @EmployeeID INT,
    @Period CHAR(7)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @pid INT = (SELECT PayrollID FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period);
    IF @pid IS NULL
    BEGIN
        SELECT 0 AS Exists, NULL AS PayrollID, 0.00 AS TotalEarnings, 0.00 AS TotalOvertime, 0.00 AS TotalSubsidies, 0.00 AS TotalDeductions, 0.00 AS NetPay;
        RETURN;
    END

    SELECT
        1 AS Exists,
        @pid AS PayrollID,
        SUM(CASE WHEN LineType='Earning'  THEN Quantity*UnitValue ELSE 0 END) AS TotalEarnings,
        SUM(CASE WHEN LineType='Overtime' THEN Quantity*UnitValue ELSE 0 END) AS TotalOvertime,
        SUM(CASE WHEN LineType='Subsidy'  THEN Quantity*UnitValue ELSE 0 END) AS TotalSubsidies,
        SUM(CASE WHEN LineType='Deduction'THEN Quantity*UnitValue ELSE 0 END) AS TotalDeductions,
        SUM(CASE WHEN LineType IN ('Earning','Overtime','Subsidy') THEN Quantity*UnitValue ELSE -Quantity*UnitValue END) AS NetPay
    FROM HR.tbl_PayrollLines
    WHERE PayrollID=@pid;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ClosePayrollPeriod
    @Period CHAR(7),
    @PaymentDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Payroll
      SET Status='Reconciled',
          PaymentDate = COALESCE(@PaymentDate, PaymentDate, CAST(SYSDATETIME() AS DATE))
    WHERE Period=@Period;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ReopenPayrollPeriod
    @Period CHAR(7)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Payroll SET Status='Pending' WHERE Period=@Period AND Status='Reconciled';
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
-- =========================================
-- 8) VIEWS
-- =========================================
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
-- =========================================
-- 9) EXTENDED PROPERTIES (COLUMN COMMENTS)
-- =========================================

/* Generate empty extended properties (MS_Description) for all columns in schema HR */
DECLARE @sql NVARCHAR(MAX)='';
SELECT @sql = STRING_AGG(CONCAT(
    'IF NOT EXISTS (SELECT 1 FROM sys.extended_properties ep ',
    'JOIN sys.columns c ON ep.major_id=c.object_id AND ep.minor_id=c.column_id ',
    'WHERE ep.name = ''MS_Description'' AND c.object_id = ', t.object_id, ' AND c.name = ''', c.name, ''') ',
    'EXEC sys.sp_addextendedproperty @name=N''MS_Description'', @value=N'''', ',
    '@level0type=N''SCHEMA'',@level0name=N''', s.name, ''', ',
    '@level1type=N''TABLE'',@level1name=N''', t.name, ''', ',
    '@level2type=N''COLUMN'',@level2name=N''', c.name, ''';'
), CHAR(10))
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id=s.schema_id
JOIN sys.columns c ON c.object_id=t.object_id
WHERE s.name='HR';
EXEC(@sql);
GO

-- =========================================
-- 10) SEED DATA
-- =========================================
-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
IF NOT EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = 'EMPLOYEE_PLAN_STATUS')
BEGIN
    INSERT INTO HR.ref_Types (Category, Name, Description) VALUES
    ('EMPLOYEE_PLAN_STATUS', 'Asignado', 'Empleado asignado a la planificación'),
    ('EMPLOYEE_PLAN_STATUS', 'En Proceso', 'Empleado ejecutando las horas asignadas'),
    ('EMPLOYEE_PLAN_STATUS', 'Ejecutado', 'Empleado completó todas las horas asignadas'),
    ('EMPLOYEE_PLAN_STATUS', 'Cancelado', 'Participación del empleado fue cancelada');
    
    PRINT 'Estados de empleado en planificación insertados en ref_Types (EMPLOYEE_PLAN_STATUS)';
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 8. INSERTAR DATOS BÁSICOS DE CONFIGURACIÓN
-------------------------------------------------------------------------------

-- 8.1 Insertar feriados básicos de Ecuador 2025
IF NOT EXISTS (SELECT 1 FROM HR.tbl_Holidays WHERE YEAR(HolidayDate) = 2025)
BEGIN
    INSERT INTO HR.tbl_Holidays (Name, HolidayDate, IsActive, Description) VALUES
    ('Año Nuevo', '2025-01-01', 1, 'Año Nuevo'),
    ('Carnaval', '2025-03-03', 1, 'Carnaval'),
    ('Carnaval', '2025-03-04', 1, 'Carnaval'),
    ('Viernes Santo', '2025-04-18', 1, 'Semana Santa'),
    ('Día del Trabajo', '2025-05-01', 1, 'Día Internacional del Trabajo'),
    ('Batalla de Pichincha', '2025-05-24', 1, 'Batalla de Pichincha'),
    ('Primer Grito de Independencia', '2025-08-10', 1, 'Primer Grito de Independencia'),
    ('Independencia de Guayaquil', '2025-10-09', 1, 'Independencia de Guayaquil'),
    ('Día de los Difuntos', '2025-11-02', 1, 'Día de los Difuntos'),
    ('Independencia de Cuenca', '2025-11-03', 1, 'Independencia de Cuenca'),
    ('Navidad', '2025-12-25', 1, 'Navidad');
    
    PRINT 'Feriados 2025 insertados';
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
/*se agrega esta parte para alterar el trigger para corregir la validacion de hora de picada */
ALTER TRIGGER [HR].[trg_Punch_Validations]
ON [HR].[tbl_AttendancePunches]
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
        JOIN HR.tbl_Vacations v
          ON v.EmployeeID = i.EmployeeID
         AND v.Status IN ('InProgress')
         AND CAST(i.PunchTime AS date) BETWEEN v.StartDate AND v.EndDate
    )
    BEGIN
        RAISERROR(N'El empleado está de vacaciones: no se permiten picadas.',16,1);
        RETURN;
    END

    -- Verificar diferencia de tiempo
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

    -- Insertar válido y capturar los IDs generados
    INSERT INTO HR.tbl_AttendancePunches(EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude, CreatedAt)
    OUTPUT INSERTED.PunchId INTO @OutputTable
    SELECT EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude, CreatedAt
    FROM inserted;

    -- Devolver los IDs generados
    SELECT PunchId FROM @OutputTable;
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
INSERT INTO HR.ref_Types (Category, Name) 
VALUES 
    ('GENDER_TYPE', 'Hombre'),
    ('GENDER_TYPE', 'Mujer'),
	('GENDER_TYPE', 'No binario'),
	('GENDER_TYPE', 'AGénero'),
	('GENDER_TYPE', 'Mujer Trans'),
	('GENDER_TYPE', 'Hombre Trans'),
	('GENDER_TYPE', 'Prefiero no decir'),
	('GENDER_TYPE', 'Otros'),
	('SEX_TYPE', 'Masculino'),
	('SEX_TYPE', 'Femenino'),
	('SEX_TYPE', 'Prefiero no decir');
	
	
	INSERT INTO HR.ref_Types (Category, Name)
	VALUES
    ('ACTION_TYPE', 'INGRESO'),
    ('ACTION_TYPE', 'REINGRESO'),
    ('ACTION_TYPE', 'RESTITUCIÓN'),
    ('ACTION_TYPE', 'ASCENSO'),
    ('ACTION_TYPE', 'TRASLADO'),
    ('ACTION_TYPE', 'TRASPASO'),
    ('ACTION_TYPE', 'CAMBIO ADMINISTRATIVO'),
    ('ACTION_TYPE', 'INTERCAMBIO VOLUNTARIO'),
    ('ACTION_TYPE', 'LICENCIA'),
    ('ACTION_TYPE', 'COMISIÓN DE SERVICIOS'),
    ('ACTION_TYPE', 'SANCIONES'),
    ('ACTION_TYPE', 'INCREMENTO RMU'),
    ('ACTION_TYPE', 'SUBROGACIÓN'),
    ('ACTION_TYPE', 'ENCARGO'),
    ('ACTION_TYPE', 'CESACIÓN DE FUNCIONES'),
    ('ACTION_TYPE', 'DESTITUCIÓN'),
    ('ACTION_TYPE', 'VACACIONES'),
    ('ACTION_TYPE', 'REVISIÓN CLASI. PUESTO'),
    ('ACTION_TYPE', 'OTRO(DETALLAR)'),
    ('ACTION_TYPE', 'NOMBRAMIENTO PROVISIONAL');
	
	/*INSERT INTO HR.ref_Types (Category, Name)
	VALUES
    ('ROW_STATUS', 'ACTIVO'),
    ('ROW_STATUS', 'INACTIVO'),
	('ACTION_STATUS', 'ACTIVO'),
    ('ACTION_STATUS', 'INACTIVO'),*/
	
	-- Tipos principales de justificación
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive) VALUES
('JUSTIFICATION', 'PUNCH', 'Justificación de picada específica (entrada/salida olvidada)', 1),
('JUSTIFICATION', 'HOURS', 'Justificación de horas específicas (ausencia parcial)', 1),
('JUSTIFICATION', 'FULL_DAY', 'Justificación de día completo (ausencia total)', 1);

-- Subtipos más específicos para mayor granularidad (opcional)
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive) VALUES
('JUSTIFICATION_REASON', 'MEDICAL', 'Cita médica o incapacidad médica', 1),
('JUSTIFICATION_REASON', 'PERSONAL', 'Asunto personal urgente', 1),
('JUSTIFICATION_REASON', 'FAMILY_EMERGENCY', 'Emergencia familiar', 1),
('JUSTIFICATION_REASON', 'TECHNICAL_ISSUE', 'Problemas técnicos (sistema, tarjeta, etc.)', 1),
('JUSTIFICATION_REASON', 'WORK_MEETING', 'Reunión de trabajo externa', 1),
('JUSTIFICATION_REASON', 'TRAINING', 'Capacitación o entrenamiento', 1),
('JUSTIFICATION_REASON', 'COURT_DUTY', 'Deber cívico (jurado, testigo, etc.)', 1),
('JUSTIFICATION_REASON', 'BEREAVEMENT', 'Duelo familiar', 1),
('JUSTIFICATION_REASON', 'TRANSPORTATION', 'Problemas de transporte', 1),
('JUSTIFICATION_REASON', 'WEATHER', 'Condiciones climáticas adversas', 1);

-- Tipos de estado para el flujo de aprobación (si quieres usar la tabla de referencia también para esto)
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive) VALUES
('JUSTIFICATION_STATUS', 'PENDING', 'Pendiente de aprobación', 1),
('JUSTIFICATION_STATUS', 'APPROVED', 'Aprobado por supervisor', 1),
('JUSTIFICATION_STATUS', 'REJECTED', 'Rechazado por supervisor', 1),
('JUSTIFICATION_STATUS', 'CANCELLED', 'Cancelado por el empleado', 1);
--('JUSTIFICATION_STATUS', 'UNDER_REVIEW', 'En revisión por RH', 1);
	
	
-- Catálogos básicos
INSERT INTO HR.tbl_OvertimeConfig(OvertimeType, Factor, Description)
VALUES
 ('Ordinaria', 1.50, 'Hora extra ordinaria'),
 ('Nocturna', 1.75, 'Hora extra nocturna'),
 ('Feriado',  2.00, 'Hora extra en feriado');

INSERT INTO HR.tbl_PermissionTypes(Name, DeductsFromVacation, RequiresApproval, MaxDays)
VALUES
 ('Vacaciones', 1, 1, 30),
 ('Comisión de servicios', 0, 1, 10),
 ('Enfermedad', 0, 1, 15),
 ('Asuntos personales', 0, 1, 5);

-- Facultades y Departamentos
INSERT INTO HR.tbl_Faculties(Name, IsActive) VALUES
 ('Ingeniería en Sistemas', 1),
 ('Jurídicas y Sociales', 1);

/*INSERT INTO HR.tbl_Departments(FacultyID, Name, IsActive) VALUES
 (1, 'DITIC', 1),
 (1, 'Académico', 1),
 (2, 'Administración', 1);*/

-- Personas
INSERT INTO HR.tbl_People(FirstName, LastName, IDCard, Email, Phone, BirthDate, Sex, Address)
VALUES
 ('Henry','Flores','1801234567','henry.flores@uta.edu.ec','099000111','1988-05-10','M','Ambato'),
 ('Ana','Pérez','1807654321','ana.perez@uta.edu.ec','098111222','1990-09-21','F','Ambato'),
 ('Luis','García','1712345678','luis.garcia@uta.edu.ec','097222333','1985-02-12','M','Quito'),
 ('María','Lozano','1711122233','maria.lozano@uta.edu.ec','096333444','1992-11-30','F','Ambato');

-- Empleados (EmployeeID = PersonID)
INSERT INTO HR.tbl_Employees(EmployeeID, Type, DepartmentID, ImmediateBossID, HireDate, IsActive)
VALUES
 (1, 'Administrative_LOSEP', 1, NULL, '2020-03-01', 1),
 (2, 'Employee_CT', 1, 1, '2022-01-15', 1),
 (3, 'Teacher_LOSE', 2, 1, '2019-09-01', 1),
 (4, 'Coordinator', 3, 1, '2021-07-01', 1);

-- Asignar decano (debe existir empleado)
UPDATE HR.tbl_Faculties SET DeanEmployeeID = 1 WHERE FacultyID = 1;

-- Schedules
INSERT INTO HR.tbl_Schedules(Description, EntryTime, ExitTime, WorkingDays, RequiredHoursPerDay, HasLunchBreak, LunchStart, LunchEnd, IsRotating)
VALUES
 ('Admin L-V 08:00-17:00', '08:00', '17:00', '1,2,3,4,5', 8.00, 1, '12:30','13:30', 0),
 ('Docente L-V 07:00-15:00', '07:00', '15:00', '1,2,3,4,5', 8.00, 1, '12:00','13:00', 0);

-- EmployeeSchedules
INSERT INTO HR.tbl_EmployeeSchedules(EmployeeID, ScheduleID, ValidFrom) VALUES
 (1, 1, '2023-01-01'),
 (2, 1, '2023-01-01'),
 (3, 2, '2023-01-01'),
 (4, 1, '2023-01-01');

-- Contratos
INSERT INTO HR.tbl_Contracts(EmployeeID, ContractType, StartDate, EndDate, BaseSalary, CreatedBy)
VALUES
 (1, 'Indefinido', '2020-03-01', NULL, 1800.00, 1),
 (2, 'Temporal',  '2022-01-15', NULL, 1200.00, 1),
 (3, 'Indefinido', '2019-09-01', NULL, 2200.00, 1),
 (4, 'Indefinido', '2021-07-01', NULL, 2000.00, 1);

-- Vacaciones (una en progreso para empleado 2)
INSERT INTO HR.tbl_Vacations(EmployeeID, StartDate, EndDate, DaysGranted, DaysTaken, Status)
VALUES
 (1, '2025-02-03', '2025-02-07', 5, 5, 'Completed'),
 (2, '2025-08-11', '2025-08-15', 5, 0, 'InProgress'); -- cuidado con picadas en estas fechas

-- Permisos
INSERT INTO HR.tbl_Permissions(EmployeeID, PermissionTypeID, StartDate, EndDate, ChargedToVacation, ApprovedBy, Justification, Status)
VALUES
 (3, 3, '2025-03-10', '2025-03-12', 0, 1, 'Reposo médico', 'Approved'),
 (4, 4, '2025-04-22', '2025-04-22', 0, 1, 'Trámite personal', 'Pending');

-- Picadas (válidas: separadas por >=5 min y fuera de vacaciones en progreso)
INSERT INTO HR.tbl_AttendancePunches(EmployeeID, PunchTime, PunchType, DeviceID)
VALUES
 (1, '2025-08-05 08:01', 'In',  'BIO-01'),
 (1, '2025-08-05 12:31', 'Out', 'BIO-01'),
 (1, '2025-08-05 13:35', 'In',  'BIO-01'),
 (1, '2025-08-05 17:05', 'Out', 'BIO-01'),
 (2, '2025-08-08 08:00', 'In',  'BIO-01'), -- 08/11-08/15 está en vacaciones; 08/08 permite
 (2, '2025-08-08 17:10', 'Out', 'BIO-01');

-- Justificación de picadas
INSERT INTO HR.tbl_PunchJustifications(PunchID, BossEmployeeID, Reason, Approved, ApprovedAt)
VALUES
 (1, 1, 'Retraso por tráfico', 1, '2025-08-05 09:00');

-- Cálculos (ejemplo)
INSERT INTO HR.tbl_AttendanceCalculations(EmployeeID, WorkDate, FirstPunchIn, LastPunchOut, TotalWorkedMinutes, RegularMinutes, OvertimeMinutes, Status)
VALUES
 (1, '2025-08-05', '2025-08-05 08:01', '2025-08-05 17:05', 544, 480, 64, 'Approved');

/* -- Horas extra (empleado 3 NO debe tener permiso ese día)
INSERT INTO HR.tbl_Overtime(EmployeeID, WorkDate, OvertimeType, Hours, Status, ApprovedBy, Factor, ActualHours, PaymentAmount)
VALUES
 (3, '2025-05-03', 'Ordinaria', 2.00, 'Planned', 1, 1.50, 0, 0); */

-- Plan de recuperación de horas
INSERT INTO HR.tbl_TimeRecoveryPlans(EmployeeID, OwedMinutes, PlanDate, FromTime, ToTime, Reason, CreatedBy)
VALUES
 (2, 60, '2025-08-18', '17:00', '18:10', 'Recuperar 1h por salida anticipada', 1);

INSERT INTO HR.tbl_TimeRecoveryLogs(RecoveryPlanID, ExecutedDate, MinutesRecovered, ApprovedBy, ApprovedAt)
VALUES
 (1, '2025-08-18', 70, 1, '2025-08-18 18:15');

-- Subrogación (sin solapamiento)
INSERT INTO HR.tbl_Subrogations(SubrogatedEmployeeID, SubrogatingEmployeeID, StartDate, EndDate, PermissionID, VacationID, Reason)
VALUES
 (1, 2, '2025-06-01', '2025-06-15', NULL, NULL, 'Cobertura por comisión');

-- Movimiento de personal
INSERT INTO HR.tbl_PersonnelMovements(EmployeeID, ContractID, OriginDepartmentID, DestinationDepartmentID, MovementDate, MovementType, Reason, CreatedBy)
VALUES
 (2, 2, 1, 2, '2025-07-01', 'Transfer', 'Rotación a área académica', 1);

-- Nómina y líneas
INSERT INTO HR.tbl_Payroll(EmployeeID, Period, BaseSalary, Status, PaymentDate, BankAccount)
VALUES
 (1, '2025-08', 1800.00, 'Paid', '2025-08-10', 'BANCO-001-XXXX'),
 (2, '2025-08', 1200.00, 'Pending', NULL, 'BANCO-002-XXXX');

INSERT INTO HR.tbl_PayrollLines(PayrollID, LineType, Concept, Quantity, UnitValue, Notes)
VALUES
 (1, 'Earning',  'Sueldo Base', 1, 1800.00, NULL),
 (1, 'Overtime', 'Horas extra',  8,   5.00, 'Pago variable'),
 (1, 'Deduction','Aporte IESS',  1,  -9.45, 'Porcentaje aplicado'),
 (2, 'Earning',  'Sueldo Base',  1, 1200.00, NULL),
 (2, 'Subsidy',  'Transporte',   1,   30.00, 'Política institucional');

PRINT 'Datos de prueba insertados correctamente.';
GO