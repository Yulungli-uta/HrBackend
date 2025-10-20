/* RESUMEN DE TABLAS CREADAS:
✅ TABLAS MAESTRAS (15):
ref_Types - Catálogo de tipos

Countries - Países

Provinces - Provincias

Cantons - Cantones

People - Personas base

Departments - Estructura organizacional

jobs - Puestos de trabajo

PermissionTypes - Tipos de permisos

OvertimeConfig - Configuración horas extra

Schedules - Horarios laborales

Holidays - Feriados

✅ TABLAS OPERATIVAS (19):
Employees - Empleados

EmployeeSchedules - Asignación horarios

Contracts - Contratos laborales

Vacations - Vacaciones

Permissions - Permisos

AttendancePunches - Registro de asistencia

PunchJustifications - Justificaciones

AttendanceCalculations - Cálculos de asistencia

Overtime - Horas extra

TimeRecoveryPlans - Planes recuperación

TimeRecoveryLogs - Logs recuperación

TimePlanning - Planificación tiempo

TimePlanningEmployees - Empleados en planificación

TimePlanningExecution - Ejecución planificación

Subrogations - Subrogaciones

PersonnelMovements - Movimientos personal

Payroll - Nómina

PayrollLines - Líneas de nómina

SalaryHistory - Historial salarial

✅ TABLAS DE SISTEMA (1):
Audit - Auditoría

✅ TABLAS DE HISTORIA DE VIDA (11):
Addresses - Direcciones

Institutions - Instituciones

EducationLevels - Niveles educativos

EmergencyContacts - Contactos emergencia

CatastrophicIllnesses - Enfermedades catastróficas

FamilyBurden - Carga familiar

Trainings - Capacitaciones

WorkExperiences - Experiencia laboral

BankAccounts - Cuentas bancarias

Publications - Publicaciones

Books - Libros

🎯 CARACTERÍSTICAS INCLUIDAS:
✅ Orden correcto de creación (sin errores de dependencia)

✅ Sin constraints (se agregan en bloques posteriores)

✅ Valores por defecto apropiados

✅ Campos de auditoría (CreatedAt, UpdatedAt, RowVersion)

✅ Estructura completa del sistema UTA-HR

*/

-- =============================================
-- BLOQUE 1: CREACIÓN DE TABLAS (SIN CONSTRAINTS)
-- CON FECHAS LOCALES - NO UTC
-- =============================================
SET NOCOUNT ON;
PRINT 'INICIANDO CREACIÓN DE TABLAS CON FECHAS LOCALES...';

-- 1. TABLA MAESTRA DE TIPOS (BASE PARA MUCHAS TABLAS)
PRINT '1. Creando HR.ref_Types...';
CREATE TABLE HR.ref_Types (
    TypeID INT IDENTITY(1,1) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

-- 2. TABLAS GEOGRÁFICAS (BASE PARA PERSONAS Y DIRECCIONES)
PRINT '2. Creando tablas geográficas...';
CREATE TABLE HR.tbl_Countries (
    CountryID NVARCHAR(10) NOT NULL,    
    CountryName NVARCHAR(100) NOT NULL,
    Nationality NVARCHAR(100) NULL,
    NationalityCode NVARCHAR(5) NULL,
    AuxSIITH NVARCHAR(5) NULL,
    AuxCEAACES NVARCHAR(5) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_Provinces (
    ProvinceID NVARCHAR(10) NOT NULL,
    CountryID NVARCHAR(10) NOT NULL,
    ProvinceName NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_Cantons (
    CantonID NVARCHAR(10) NOT NULL,
    ProvinceID NVARCHAR(10) NOT NULL,
    CantonName NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

-- 3. TABLA DE PERSONAS (BASE PARA EMPLEADOS)
PRINT '3. Creando HR.tbl_People...';
CREATE TABLE HR.tbl_People (
    PersonID INT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    IdentType INT NULL,
    IDCard NVARCHAR(20) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(30) NULL,
    BirthDate DATE NULL,
    Sex INT NULL,
    Gender INT NULL,
    Disability NVARCHAR(200) NULL,
    Address NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    UpdatedAt DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    MaritalStatusTypeID INT NULL,
    MilitaryCard NVARCHAR(50) NULL,
    MotherName NVARCHAR(100) NULL,
    FatherName NVARCHAR(100) NULL,
    CountryID NVARCHAR(10) NULL,
    ProvinceID NVARCHAR(10) NULL,
    CantonID NVARCHAR(10) NULL,
    YearsOfResidence INT NULL,
    EthnicityTypeID INT NULL,
    BloodTypeTypeID INT NULL,
    SpecialNeedsTypeID INT NULL,
    DisabilityPercentage DECIMAL(5,2) NULL,
    CONADISCard NVARCHAR(50) NULL,
    RowVersion ROWVERSION
);

-- 4. TABLA DE DEPARTAMENTOS (ESTRUCTURA JERÁRQUICA)
PRINT '4. Creando HR.tbl_Departments...';
CREATE TABLE HR.tbl_Departments (
    DepartmentID INT IDENTITY(1,1) NOT NULL,
    ParentID INT NULL,
    Code NVARCHAR(20) NOT NULL,
    Name NVARCHAR(120) NOT NULL,
    ShortName NVARCHAR(50) NULL,
    DepartmentType INT NOT NULL,
    Email NVARCHAR(100) NULL,
    Phone NVARCHAR(20) NULL,
    Location NVARCHAR(200) NULL,
    DeanDirector INT NULL,
    BudgetCode NVARCHAR(30) NULL,
    Dlevel INT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    UpdatedAt DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    RowVersion ROWVERSION
);

-- 5. TABLA DE PUESTOS DE TRABAJO
PRINT '5.1. Creando HR.tbl_Degrees...';
CREATE TABLE HR.tbl_Degrees (
    DegreeID INT IDENTITY(1,1) NOT NULL,
    Description NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(GETDATE()),
    UpdatedAt DATETIME2(0) NULL
);
PRINT '5.2. Creando HR.tbl_Degrees...';
CREATE TABLE HR.tbl_Occupational_Groups(
    GroupID INT IDENTITY(1,1) NOT NULL,
    Description NVARCHAR(200) NOT NULL,
    RMU DECIMAL(10, 2) NOT NULL,
    DegreeID INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2(0) NOT NULL DEFAULT(GETDATE()),
    UpdatedAt DATETIME2(0) NULL
);
PRINT '5.3. Creando HR.tbl_jobs...';
CREATE TABLE HR.tbl_jobs (
    JobID INT IDENTITY(1,1) NOT NULL,
    --Title NVARCHAR(255) NOT NULL,
    Description TEXT NULL,
	JobTypeID INT NULL,   --REFTYPE.TYPEID --
    GroupID INT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
	UpdatedAt DATETIME2(0) NULL
);

CREATE TABLE HR.tbl_Activities (
    ActivitiesID INT IDENTITY(1,1) NOT NULL,    
    Description TEXT NULL,
	ActivitiesType NVARCHAR(20) NOT NULL DEFAULT('LABORAL') CHECK (ActivitiesType IN ('LABORAL','ADICIONAL')),
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
	UpdatedAt DATETIME2(0) NULL
);

CREATE TABLE HR.tbl_JobActivities (
    ActivitiesID INT NOT NULL,    
    JobID INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
	UpdatedAt DATETIME2(0) NULL
);

CREATE TABLE hr.tbl_contract_type (
	ContractTypeID INT IDENTITY,
	PersonalContractTypeID INT NULL,  --REFTYPE.TYPEID
	name NVARCHAR(100) NOT NULL,
	description NVARCHAR(150) NULL,
	Status VARCHAR(1) NOT NULL,
	ContractText NVARCHAR(MAX) NULL,	 
	ContractCode NVARCHAR(30) NULL	 --UNIQUE
);


CREATE TABLE HR.tbl_AdditionalActivities (
    ActivitiesID INT NOT NULL,     --tbl_Activities.ActivitiesID
	ContractID INT NOT NULL,       --CONTRACT 
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
	UpdatedAt DATETIME2(0) NULL
);



-- 6. TABLA DE EMPLEADOS
PRINT '6. Creando HR.tbl_Employees...';
CREATE TABLE HR.tbl_Employees (
    EmployeeID INT IDENTITY(1,1) NOT NULL,
	PersonID INT NOT NULL, 
    EmployeeType INT NULL,
    DepartmentID INT NULL,
    ImmediateBossID INT NULL,
    HireDate DATE NOT NULL,
    Email NVARCHAR(150) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedBy INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    UpdatedBy INT NULL,
    UpdatedAt DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    RowVersion ROWVERSION
);

-- 7. TABLAS DE CONFIGURACIÓN DEL SISTEMA
PRINT '7. Creando tablas de configuración...';
CREATE TABLE HR.tbl_PermissionTypes (
    TypeID INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(80) NOT NULL,
    DeductsFromVacation BIT NOT NULL DEFAULT(0),
    RequiresApproval BIT NOT NULL DEFAULT(1),
    MaxDays INT NULL
);

CREATE TABLE HR.tbl_OvertimeConfig (
    OvertimeType NVARCHAR(50) NOT NULL,
    Factor DECIMAL(5,2) NOT NULL,
    Description NVARCHAR(200) NULL
);

CREATE TABLE HR.tbl_Schedules (
    ScheduleID INT IDENTITY(1,1) NOT NULL,
    Description NVARCHAR(120) NOT NULL,
    EntryTime TIME NOT NULL,
    ExitTime TIME NOT NULL,
    WorkingDays NVARCHAR(20) NOT NULL,
    RequiredHoursPerDay DECIMAL(5,2) NOT NULL,
    HasLunchBreak BIT NOT NULL DEFAULT(1),
    LunchStart TIME NULL,
    LunchEnd TIME NULL,
    IsRotating BIT NOT NULL DEFAULT(0),
    RotationPattern NVARCHAR(120) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    UpdatedAt DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_Holidays (
    HolidayID INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    HolidayDate DATE NOT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

-- 8. TABLAS OPERATIVAS PRINCIPALES
PRINT '8. Creando tablas operativas principales...';
CREATE TABLE HR.tbl_EmployeeSchedules (
    EmpScheduleID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    ScheduleID INT NOT NULL,
    ValidFrom DATE NOT NULL,
    ValidTo DATE NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_contractRequest (
	RequestID INT IDENTITY(1,1) NOT NULL,
	WorkModalityID INT NULL,--modalidad de trabajo CATEGORY WORK_MODALITY hr.reftype.typeid
	DepartmentID INT NULL,  --Departamento solicitante 
	NumberOfPeopleToHire INT NOT NULL DEFAULT (0), 	 --numero de personas a requerir 
	NumberHour DECIMAL(12,2) NOT NULL DEFAULT (0), --numerod e horas 
	TotalPeopleHired INT NOT NULL DEFAULT (0), --numero total de personas ya contratadas 
	CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
	CreatedBy INT NOT NULL, -- persona que solicita 
	UpdateAt DATETIME2 NULL, 
	UpdateBy INT NULL,	
	Status INT NULL --ESTADO Category CONTRACT_REQUEST_STATUS
);


Create Table Hr.tbl_FinancialCertification(
	CertificationID INT IDENTITY(1,1) NOT NULL,
	RequestID INT NULL,						--tbl_contractRequest.RequestID
	CertCode NVARCHAR(100) NOT NULL, 		-- CODIGO de certificación presupuestaria
	CertNumber NVARCHAR(100) NULL,				-- NUMERO de certificación presupuestaria
	budget NVARCHAR(100) NULL, 						-- Número de partida presupuestaria
	CertBudgetDate DATETIME2 NULL,					-- Fecha de certificación presupuestaria           
    -- REMUNERACIÓN
	rmu_hour DECIMAL(12,2) NULL,                 -- Remuneración por hora
	rmu_con DECIMAL(12,2) NULL,   
	CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
	CreatedBy INT NOT NULL,
	UpdateAt DATETIME2 NULL, 
	UpdateBy INT NULL,
	Status INT NULL 			--ESTADO Category CERTF_FINANCIAL
  );



CREATE TABLE HR.tbl_Contracts (
    ContractID INT IDENTITY(1,1) NOT NULL,
	CertificationID INT NULL,				--
	ParentID INT NULL,
	ContractCode NVARCHAR(30) NOT NULL, 	--NUMERO O CODIGO DE CONTRATO
	-- TIPO DE DOCUMENTO
    --AdendumType CHAR(1) NULL,                     -- Tipo de adendum/modificación
    --EmployeeID INT NOT NULL,
	PersonID INT NOT NULL,					--CODIGO DE LA PERSONA TBL_PERSON
    ContractTypeID INT NOT NULL,			--TBL_CONTRACT_TYPE.ContractTypeID
    JobID INT NULL,							--PUESTO AL QUE VA A SER CONTRATADO tbl_jobs.JobID 
	--FECHAS DEL CONTRATO
	startdate DATETIME2 NOT NULL,                -- Fecha de inicio del contrato
	enddate DATETIME2 NOT NULL,               -- Fecha de fin del contrato
	
	ContractFileName NVARCHAR(200) NULL,		--NOMBRE DEL ARCHIVO DE CONTRATO
	ContractFilepath NVARCHAR(MAX) NULL,		--UBICACION DEL ARCHIVO DE CONTRATO	
	Status INT NOT NULL DEFAULT (0), 			--estado: 0=Iniciado, 1=Modificado, 2=Aprobado,3=Firmado&Finalizado, 4=Anulado, 5=Renuncia	
	ContractDescription NVARCHAR(MAX) NULL,     -- Explicación/descripción del contrato
	DepartmentID INT NOT NULL,					--el lugar de dependincia del cargo tbl_Departments.DepartmentID    
	authorizationdate	DATETIME2 NULL, 		--fecha de autorizacion
	
	-- ===== CAMPOS PARA RENUNCIA =====
	ResignationFileName NVARCHAR(150) NULL,        -- Nombre del PDF de renuncia - namepdf_renun_con
	ResignationFilepath NVARCHAR(250) NULL,        -- Ruta del PDF de renuncia - pdf_path_renun
	ResignationCode NVARCHAR(20) NULL,        -- Número de registro de renuncia - numregister_renun_con
	RegResignationdate DATETIME2 NULL,      		   	-- Fecha de registro de renuncia - registrationdate_renun_con
	Resignationdate DATETIME2 NULL,             	-- Fecha de renuncia (si aplica)	
	
	-- ===== CAMPOS PARA ANULACIÓN =====
	Cancelreason NVARCHAR(250) NULL,    				-- Razón de anulación del contrato - nullification_reason_con
	CancelFilename NVARCHAR(150) NULL,            -- Nombre del PDF de anulación - namepdf_anul_con
	CancelFilepath NVARCHAR(250) NULL,               -- Ruta del PDF de anulación - pdf_path_anul
	CancelCode NVARCHAR(20) NULL,         -- Número de registro de anulación - numregister_anul_con
	registrationdate_anul_con DATETIME2 NULL,       -- Fecha de registro de anulación - registrationdate_anul_con
  
	
	-- ===== INFORMACIÓN INTERNACIONAL =====
	nationality NVARCHAR(100) NULL,             -- Nacionalidad del contratado
	visa NVARCHAR(150) NULL,                        -- Información de visa (extranjeros)
	consulate NVARCHAR(150) NULL,                   -- Consulado relacionado
	work_of NVARCHAR(150) NULL,     
	
	-- ===== CONTENIDO DEL DOCUMENTO =====
	InicialContent NVARCHAR(MAX) NULL,
	ResolucionContent NVARCHAR(MAX) NULL,
	
	-- RELACIONES Y COMPETENCIAS
	relationshiptype INT NULL,          -- 0=Con dependencia, 1=Sin dependencia, 2=Otro
	relationship NVARCHAR(500) NULL,                -- Descripción de relaciones
	competition NVARCHAR(800) NULL,                 -- Competencias requeridas
	competitionDate DATETIME2 NULL,                -- Fecha de competencia
	
    /*
	DocumentNum NVARCHAR(50) NOT NULL,
    Motivation NVARCHAR(MAX) NULL,
    BudgetItem NVARCHAR(50) NULL,
    Grade INT NULL,
    GovernanceLevel NVARCHAR(MAX) NULL,
    Workplace NVARCHAR(500) NULL,
    BaseSalary DECIMAL(12,2) NOT NULL,

	  -- ===== CAMPOS ESPECÍFICOS PARA INVESTIGADORES =====
	  case_inv_con VARCHAR(250) NULL,                -- Nombre del proyecto de investigación
	  responsable_inve_con VARCHAR(100) NULL,        -- Responsable del proyecto de investigación
	  adminresolution_inv_con VARCHAR(50) NULL,      -- Número de resolución administrativa
	  adminresolutiondate_inv_con DATETIME NULL,     -- Fecha de resolución administrativa
	  officedate_inv_con DATETIME NULL,              -- Fecha de oficio DIDE (Dirección Investigación)
	  internalreport_inv_con VARCHAR(50) NULL,       -- Número de informe interno de aprobación DTH
	  internalreportdate_inv_con DATETIME NULL,      -- Fecha de informe interno de aprobación DTH
	  office_inv_con VARCHAR(50) NULL,               -- Número de oficio para investigador (DIDE)
	*/
    CreatedBy INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    UpdatedBy INT NULL,
    UpdatedAt DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_contractAttachedFile (
	AttachedID INT IDENTITY(1,1) NOT NULL,	
	ContractID INT NULL,			--tBL_CONTRACT.ContractID
	typeID  INT NULL,				--reftype.typeID CATEGORY CONTRAC_FILE_TYPE
	Description NVARCHAR(200) NULL, 
	filename NVARCHAR(150) NULL,   
	filepath NVARCHAR(max) NULL,   
	IsActive BIT NOT NULL DEFAULT(1),  
	CreatedBy INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
);



CREATE TABLE HR.tbl_Vacations (
    VacationID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    DaysGranted INT NOT NULL,
    DaysTaken INT NOT NULL DEFAULT(0),
    Status NVARCHAR(20) NOT NULL DEFAULT('Planned'),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    UpdatedAt DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_Permissions (
    PermissionID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    PermissionTypeID INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    ChargedToVacation BIT NOT NULL DEFAULT(0),
    ApprovedBy INT NULL,
    Justification NVARCHAR(MAX) NULL,
    RequestDate DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    Status NVARCHAR(20) NOT NULL DEFAULT('Pending'),
    VacationID INT NULL,
    RowVersion ROWVERSION
);

-- 9. TABLAS DE ASISTENCIA Y CONTROL
PRINT '9. Creando tablas de asistencia...';
CREATE TABLE HR.tbl_AttendancePunches (
    PunchID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    PunchTime DATETIME2 NOT NULL,                     -- CAMBIADO A DATETIME2
    PunchType NVARCHAR(10) NOT NULL,
    DeviceID NVARCHAR(60) NULL,
    Longitude DECIMAL(10,7) NULL,
    Latitude DECIMAL(10,7) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_PunchJustifications (
    PunchJustID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    BossEmployeeID INT NOT NULL,
    JustificationTypeID INT NOT NULL,
    StartDATETIME2 DATETIME2 NULL,                     -- CAMBIADO A DATETIME2
    EndDATETIME2 DATETIME2 NULL,                       -- CAMBIADO A DATETIME2
    JustificationDate DATE NULL,
    Reason NVARCHAR(500) NOT NULL,
    HoursRequested DECIMAL(4,2) NULL,
    Approved BIT NOT NULL DEFAULT(0),
    ApprovedAt DATETIME2 NULL,                        -- CAMBIADO A DATETIME2
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    CreatedBy INT NOT NULL,
    Comments NVARCHAR(1000) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT('PENDING')
);

CREATE TABLE HR.tbl_AttendanceCalculations (
    CalculationID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    WorkDate DATE NOT NULL,
    FirstPunchIn DATETIME2 NULL,                      -- CAMBIADO A DATETIME2
    LastPunchOut DATETIME2 NULL,                      -- CAMBIADO A DATETIME2
    TotalWorkedMinutes INT NOT NULL DEFAULT(0),
    RegularMinutes INT NOT NULL DEFAULT(0),
    OvertimeMinutes INT NOT NULL DEFAULT(0),
    NightMinutes INT NOT NULL DEFAULT(0),
    HolidayMinutes INT NOT NULL DEFAULT(0),
    Status NVARCHAR(12) NOT NULL DEFAULT('Pending')
);

-- 10. TABLAS DE HORAS EXTRA Y RECUPERACIÓN
PRINT '10. Creando tablas de horas extra...';
CREATE TABLE HR.tbl_Overtime (
    OvertimeID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    WorkDate DATE NOT NULL,
    OvertimeType NVARCHAR(50) NOT NULL,
    Hours DECIMAL(5,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT('Planned'),
    ApprovedBy INT NULL,
    SecondApprover INT NULL,
    Factor DECIMAL(5,2) NOT NULL,
    ActualHours DECIMAL(5,2) NOT NULL DEFAULT(0),
    PaymentAmount DECIMAL(12,2) NOT NULL DEFAULT(0),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_TimeRecoveryPlans (
    RecoveryPlanID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    OwedMinutes INT NOT NULL,
    PlanDate DATE NOT NULL,
    FromTime TIME NOT NULL,
    ToTime TIME NOT NULL,
    Reason NVARCHAR(300) NULL,
    CreatedBy INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_TimeRecoveryLogs (
    RecoveryLogID INT IDENTITY(1,1) NOT NULL,
    RecoveryPlanID INT NOT NULL,
    ExecutedDate DATE NOT NULL,
    MinutesRecovered INT NOT NULL,
    ApprovedBy INT NULL,
    ApprovedAt DATETIME2 NULL                         -- CAMBIADO A DATETIME2
);

-- 11. TABLAS DE PLANIFICACIÓN DE TIEMPO (NUEVAS)
PRINT '11. Creando tablas de planificación de tiempo...';
CREATE TABLE HR.tbl_TimePlanning (
    PlanID INT IDENTITY(1,1) NOT NULL,
    PlanType NVARCHAR(20) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    OvertimeType NVARCHAR(50) NULL,
    Factor DECIMAL(5,2) NULL,
    OwedMinutes INT NULL,
    PlanStatusTypeID INT NOT NULL,
    RequiresApproval BIT NOT NULL DEFAULT(1),
    ApprovedBy INT NULL,
    SecondApprover INT NULL,
    ApprovedAt DATETIME2 NULL,                        -- CAMBIADO A DATETIME2
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    UpdatedBy INT NULL,
    UpdatedAt DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_TimePlanningEmployees (
    PlanEmployeeID INT IDENTITY(1,1) NOT NULL,
    PlanID INT NOT NULL,
    EmployeeID INT NOT NULL,
    AssignedHours DECIMAL(5,2) NULL,
    AssignedMinutes INT NULL,
    ActualHours DECIMAL(5,2) NULL DEFAULT(0),
    ActualMinutes INT NULL DEFAULT(0),
    EmployeeStatusTypeID INT NOT NULL,
    PaymentAmount DECIMAL(12,2) NULL DEFAULT(0),
    IsEligible BIT NOT NULL DEFAULT(1),
    EligibilityReason NVARCHAR(300) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_TimePlanningExecution (
    ExecutionID INT IDENTITY(1,1) NOT NULL,
    PlanEmployeeID INT NOT NULL,
    WorkDate DATE NOT NULL,
    StartTime DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    EndTime DATETIME2 NULL,                           -- CAMBIADO A DATETIME2
    TotalMinutes INT NOT NULL DEFAULT(0),
    RegularMinutes INT NOT NULL DEFAULT(0),
    OvertimeMinutes INT NOT NULL DEFAULT(0),
    NightMinutes INT NOT NULL DEFAULT(0),
    HolidayMinutes INT NOT NULL DEFAULT(0),
    VerifiedBy INT NULL,
    VerifiedAt DATETIME2 NULL,                        -- CAMBIADO A DATETIME2
    Comments NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

-- 12. TABLAS DE MOVIMIENTOS Y SUBROGACIONES
PRINT '12. Creando tablas de movimientos...';
CREATE TABLE HR.tbl_Subrogations (
    SubrogationID INT IDENTITY(1,1) NOT NULL,
    SubrogatedEmployeeID INT NOT NULL,
    SubrogatingEmployeeID INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    PermissionID INT NULL,
    VacationID INT NULL,
    Reason NVARCHAR(300) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_PersonnelMovements (
    MovementID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    ContractID INT NOT NULL,
    JobID INT NOT NULL,
    OriginDepartmentID INT NULL,
    DestinationDepartmentID INT NOT NULL,
    MovementDate DATE NULL,
    MovementType NVARCHAR(30) NULL,
    DocumentLocation NVARCHAR(255) NULL,
    Reason NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedBy INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    RowVersion ROWVERSION
);

-- 13. TABLAS DE NÓMINA
PRINT '13. Creando tablas de nómina...';
CREATE TABLE HR.tbl_Payroll (
    PayrollID INT IDENTITY(1,1) NOT NULL,
    EmployeeID INT NOT NULL,
    Period CHAR(7) NOT NULL,
    BaseSalary DECIMAL(12,2) NOT NULL,
    Status NVARCHAR(15) NOT NULL DEFAULT('Pending'),
    PaymentDate DATE NULL,
    BankAccount NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    UpdatedAt DATETIME2 NULL,                         -- CAMBIADO A DATETIME2
    RowVersion ROWVERSION
);

CREATE TABLE HR.tbl_PayrollLines (
    PayrollLineID INT IDENTITY(1,1) NOT NULL,
    PayrollID INT NOT NULL,
    LineType NVARCHAR(20) NOT NULL,
    Concept NVARCHAR(120) NOT NULL,
    Quantity DECIMAL(10,2) NOT NULL DEFAULT(1),
    UnitValue DECIMAL(12,2) NOT NULL DEFAULT(0),
    Notes NVARCHAR(300) NULL
);

CREATE TABLE HR.tbl_SalaryHistory (
    SalaryHistoryID INT IDENTITY(1,1) NOT NULL,
    ContractID INT NOT NULL,
    OldSalary DECIMAL(12,2) NOT NULL,
    NewSalary DECIMAL(12,2) NOT NULL,
    ChangedBy SYSNAME NOT NULL DEFAULT(SUSER_SNAME()),
    ChangedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    Reason NVARCHAR(300) NULL
);

-- 14. TABLA DE AUDITORÍA
PRINT '14. Creando HR.tbl_Audit...';
CREATE TABLE HR.tbl_Audit (
    AuditID BIGINT IDENTITY(1,1) NOT NULL,
    TableName SYSNAME NOT NULL,
    Action NVARCHAR(20) NOT NULL,
    RecordID NVARCHAR(64) NOT NULL,
    UserName SYSNAME NOT NULL DEFAULT(SUSER_SNAME()),
    DATETIME2 DATETIME2 NOT NULL DEFAULT(GETDATE()),  -- CAMBIADO A GETDATE()
    Details NVARCHAR(MAX) NULL
);

-- 15. TABLAS DE HISTORIA DE VIDA
PRINT '15. Creando tablas de historia de vida...';
CREATE TABLE HR.tbl_Addresses (
    AddressID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    AddressTypeID INT NOT NULL,
    CountryID NVARCHAR(10) NOT NULL,
    ProvinceID NVARCHAR(10) NOT NULL,
    CantonID NVARCHAR(10) NOT NULL,
    Parish NVARCHAR(100) NULL,
    Neighborhood NVARCHAR(100) NULL,
    MainStreet NVARCHAR(100) NOT NULL,
    SecondaryStreet NVARCHAR(100) NULL,
    HouseNumber NVARCHAR(20) NULL,
    Reference NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_Institutions (
    InstitutionID INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    InstitutionTypeID INT NOT NULL,
    CountryID NVARCHAR(10) NOT NULL,
    ProvinceID NVARCHAR(10) NOT NULL,
    CantonID NVARCHAR(10) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_EducationLevels (
    EducationID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    EducationLevelTypeID INT NOT NULL,
    InstitutionID INT NOT NULL,
    Title NVARCHAR(150) NOT NULL,
    Specialty NVARCHAR(100) NULL,
    StartDate DATE NULL,
    EndDate DATE NULL,
    Grade NVARCHAR(50) NULL,
    Location NVARCHAR(100) NULL,
    Score DECIMAL(5,2) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_EmergencyContacts (
    ContactID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Identification NVARCHAR(20) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    RelationshipTypeID INT NOT NULL,
    Address NVARCHAR(255) NULL,
    Phone NVARCHAR(30) NULL,
    Mobile NVARCHAR(30) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_CatastrophicIllnesses (
    IllnessID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Illness NVARCHAR(150) NOT NULL,
    IESSNumber NVARCHAR(50) NULL,
    SubstituteName NVARCHAR(100) NULL,
    IllnessTypeID INT NOT NULL,
    CertificateNumber NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_FamilyBurden (
    BurdenID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    DependentID NVARCHAR(20) NOT NULL,
    IdentificationTypeID INT NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    BirthDate DATE NOT NULL,
    DisabilityTypeID INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_Trainings (
    TrainingID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Location NVARCHAR(100) NULL,
    Title NVARCHAR(200) NOT NULL,
    Institution NVARCHAR(150) NOT NULL,
    KnowledgeAreaTypeID INT NULL,
    EventTypeID INT NULL,
    CertifiedBy NVARCHAR(150) NULL,
    CertificateTypeID INT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Hours INT NOT NULL,
    ApprovalTypeID INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_WorkExperiences (
    WorkExpID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    CountryID NVARCHAR(10) NULL,
    Company NVARCHAR(150) NOT NULL,
    InstitutionTypeID INT NULL,
    EntryReason NVARCHAR(200) NULL,
    ExitReason NVARCHAR(200) NULL,
    Position NVARCHAR(120) NOT NULL,
    InstitutionAddress NVARCHAR(255) NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    ExperienceTypeID INT NULL,
    IsCurrent BIT NOT NULL DEFAULT(0),
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_BankAccounts (
    AccountID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    FinancialInstitution NVARCHAR(150) NOT NULL,
    AccountTypeID INT NOT NULL,
    AccountNumber NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_Publications (
    PublicationID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Location NVARCHAR(100) NULL,
    PublicationTypeID INT NULL,
    IsIndexed BIT NULL,
    JournalTypeID INT NULL,
    ISSN_ISBN NVARCHAR(20) NULL,
    JournalName NVARCHAR(200) NULL,
    JournalNumber NVARCHAR(50) NULL,
    Volume NVARCHAR(50) NULL,
    Pages NVARCHAR(20) NULL,
    KnowledgeAreaTypeID INT NULL,
    SubAreaTypeID INT NULL,
    AreaTypeID INT NULL,
    Title NVARCHAR(300) NOT NULL,
    OrganizedBy NVARCHAR(150) NULL,
    EventName NVARCHAR(200) NULL,
    EventEdition NVARCHAR(50) NULL,
    PublicationDate DATE NULL,
    UTAffiliation BIT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

CREATE TABLE HR.tbl_Books (
    BookID INT IDENTITY(1,1) NOT NULL,
    PersonID INT NOT NULL,
    Title NVARCHAR(300) NOT NULL,
    PeerReviewed BIT NULL,
    ISBN NVARCHAR(20) NULL,
    Publisher NVARCHAR(200) NULL,
    CountryID NVARCHAR(10) NULL,
    City NVARCHAR(100) NULL,
    KnowledgeAreaTypeID INT NULL,
    SubAreaTypeID INT NULL,
    AreaTypeID INT NULL,
    VolumeCount INT NULL,
    ParticipationTypeID INT NULL,
    PublicationDate DATE NULL,
    UTAffiliation BIT NULL,
    UTASponsorship BIT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())  -- CAMBIADO A GETDATE()
);

PRINT 'TODAS LAS TABLAS CREADAS EXITOSAMENTE CON FECHAS LOCALES.';
GO