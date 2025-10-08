-- CREATE TABLE extraídos (balanceo de paréntesis) 
GO

CREATE TABLE (sin PK/FK)  2) ALTER (PK/FK/UNIQUE)
GO

CREATE TABLE HR.ref_Types (
    TypeID INT IDENTITY(1,1) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

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

CREATE TABLE HR.tbl_Departments(
    DepartmentID    INT IDENTITY(1,1) NOT NULL,
    FacultyID       INT             NULL,
    Name            VARCHAR(120)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2(0)    NULL,
    RowVersion      ROWVERSION
);
GO

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
GO

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

CREATE TABLE HR.tbl_PermissionTypes(
    TypeID              INT IDENTITY(1,1) NOT NULL,
    Name                VARCHAR(80) NOT NULL,
    DeductsFromVacation BIT NOT NULL DEFAULT(0),
    RequiresApproval    BIT NOT NULL DEFAULT(1),
    MaxDays             INT NULL
);
GO

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

CREATE TABLE HR.tbl_PunchJustifications(
    PunchJustID     INT IDENTITY(1,1) NOT NULL,
    PunchID         INT NOT NULL,
    BossEmployeeID  INT NOT NULL,
    Reason          VARCHAR(500) NOT NULL,
    Approved        BIT NOT NULL DEFAULT(0),
    ApprovedAt      DATETIME2(0) NULL
);*/
GO

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

CREATE TABLE HR.tbl_OvertimeConfig(
    OvertimeType    VARCHAR(50) NOT NULL, -- PK luego
    Factor          DECIMAL(5,2) NOT NULL,
    Description     VARCHAR(200) NULL
);
GO

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

CREATE TABLE HR.tbl_TimeRecoveryLogs(
    RecoveryLogID   INT IDENTITY(1,1) NOT NULL,
    RecoveryPlanID  INT NOT NULL,
    ExecutedDate    DATE NOT NULL,
    MinutesRecovered INT NOT NULL,
    ApprovedBy      INT NULL,
    ApprovedAt      DATETIME2(0) NULL
);
GO

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
GO

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
GO

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
GO

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
GO

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

CREATE TABLE HR.ref_Types (
    TypeID INT IDENTITY(1,1) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

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

CREATE TABLE HR.tbl_Provinces (
    ProvinceID VARCHAR(10) NOT NULL,
    CountryID VARCHAR(10) NOT NULL,
    ProvinceName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

CREATE TABLE HR.tbl_Cantons (
    CantonID VARCHAR(10) NOT NULL,
    ProvinceID VARCHAR(10) NOT NULL,
    CantonName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

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

CREATE TABLE HR.tbl_BankAccounts (
    AccountID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    FinancialInstitution VARCHAR(150) NOT NULL,
    AccountTypeID INT NOT NULL,
    AccountNumber VARCHAR(50) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
);
GO

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

