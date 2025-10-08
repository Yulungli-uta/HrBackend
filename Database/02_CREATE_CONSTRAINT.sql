/*
📊 RESUMEN DE CONSTRAINTS CREADOS:
✅ PRIMARY KEYS: 42 PKs
Una por cada tabla del sistema

✅ UNIQUE CONSTRAINTS: 6+ Unique
UQ_ref_Types_CategoryName

UQ_People_IDCard, UQ_People_Email

UQ_Departments_Code

UQ_Employees_Email

UQ_Payroll_EmployeePeriod

UX_Holidays_Date (Filtered Index)

✅ CHECK CONSTRAINTS: 25+ Checks
Validación de estados (Status)

Validación de fechas

Validación de valores positivos

Validación de tipos específicos

✅ DEFAULT CONSTRAINTS: 40+ Defaults
Valores por defecto para estados

Valores por defecto para fechas

Valores por defecto para campos numéricos

Valores por defecto para campos booleanos

✅ FOREIGN KEYS: 85+ FKs
Todas las relaciones entre tablas

En orden correcto de dependencias

Con ON DELETE CASCADE donde es apropiado

🎯 ORDEN CORRECTO DE EJECUCIÓN:
PRIMARY KEYS → Identificadores únicos

UNIQUE CONSTRAINTS → Evitar duplicados

CHECK CONSTRAINTS → Validación de datos

DEFAULT CONSTRAINTS → Valores por defecto

FOREIGN KEYS → Relaciones entre tablas
*/
-- =============================================
-- BLOQUE 2: CONSTRAINTS EN ORDEN CORRECTO
-- =============================================
SET NOCOUNT ON;
PRINT 'INICIANDO CREACIÓN DE CONSTRAINTS...';

-- =============================================
-- 2.1 PRIMARY KEYS
-- =============================================
PRINT '2.1. Agregando PRIMARY KEYS...';

-- Tablas maestras
ALTER TABLE HR.ref_Types ADD CONSTRAINT PK_ref_Types PRIMARY KEY (TypeID);
ALTER TABLE HR.tbl_Countries ADD CONSTRAINT PK_Countries PRIMARY KEY (CountryID);
ALTER TABLE HR.tbl_Provinces ADD CONSTRAINT PK_Provinces PRIMARY KEY (ProvinceID);
ALTER TABLE HR.tbl_Cantons ADD CONSTRAINT PK_Cantons PRIMARY KEY (CantonID);
ALTER TABLE HR.tbl_People ADD CONSTRAINT PK_People PRIMARY KEY (PersonID);
ALTER TABLE HR.tbl_Departments ADD CONSTRAINT PK_Departments PRIMARY KEY (DepartmentID);

-- Tablas de clasificación laboral
ALTER TABLE HR.tbl_Degrees ADD CONSTRAINT PK_Degrees PRIMARY KEY (DegreeID);
ALTER TABLE HR.tbl_Occupational_Groups ADD CONSTRAINT PK_Occupational_Groups PRIMARY KEY (GroupID);
ALTER TABLE HR.tbl_jobs ADD CONSTRAINT PK_jobs PRIMARY KEY (JobID);
ALTER TABLE HR.tbl_Activities ADD CONSTRAINT PK_Activities PRIMARY KEY (ActivitiesID);
ALTER TABLE HR.tbl_JobActivities ADD CONSTRAINT PK_JobActivities PRIMARY KEY (ActivitiesID, tbl_jobs);
ALTER TABLE HR.tbl_contract_type ADD CONSTRAINT PK_contract_type PRIMARY KEY (ContractTypeID);

-- Tablas de empleados
ALTER TABLE HR.tbl_Employees ADD CONSTRAINT PK_Employees PRIMARY KEY (EmployeeID);

-- Tablas de configuración
ALTER TABLE HR.tbl_PermissionTypes ADD CONSTRAINT PK_PermissionTypes PRIMARY KEY (TypeID);
ALTER TABLE HR.tbl_OvertimeConfig ADD CONSTRAINT PK_OvertimeConfig PRIMARY KEY (OvertimeType);
ALTER TABLE HR.tbl_Schedules ADD CONSTRAINT PK_Schedules PRIMARY KEY (ScheduleID);
ALTER TABLE HR.tbl_Holidays ADD CONSTRAINT PK_Holidays PRIMARY KEY (HolidayID);

-- Tablas operativas principales
ALTER TABLE HR.tbl_EmployeeSchedules ADD CONSTRAINT PK_EmployeeSchedules PRIMARY KEY (EmpScheduleID);
ALTER TABLE HR.tbl_contractRequest ADD CONSTRAINT PK_contractRequest PRIMARY KEY (RequestID);
ALTER TABLE HR.tbl_FinancialCertification ADD CONSTRAINT PK_FinancialCertification PRIMARY KEY (CertificationID);
ALTER TABLE HR.tbl_Contracts ADD CONSTRAINT PK_Contracts PRIMARY KEY (ContractID);
ALTER TABLE HR.tbl_AdditionalActivities ADD CONSTRAINT PK_AdditionalActivities PRIMARY KEY (ActivitiesID, ContractID);
ALTER TABLE HR.tbl_contractAttachedFile ADD CONSTRAINT PK_contractAttachedFile PRIMARY KEY (AttachedID);
ALTER TABLE HR.tbl_Vacations ADD CONSTRAINT PK_Vacations PRIMARY KEY (VacationID);
ALTER TABLE HR.tbl_Permissions ADD CONSTRAINT PK_Permissions PRIMARY KEY (PermissionID);

-- Tablas de asistencia
ALTER TABLE HR.tbl_AttendancePunches ADD CONSTRAINT PK_AttendancePunches PRIMARY KEY (PunchID);
ALTER TABLE HR.tbl_PunchJustifications ADD CONSTRAINT PK_PunchJustifications PRIMARY KEY (PunchJustID);
ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT PK_AttendanceCalculations PRIMARY KEY (CalculationID);

-- Tablas de horas extra y recuperación
ALTER TABLE HR.tbl_Overtime ADD CONSTRAINT PK_Overtime PRIMARY KEY (OvertimeID);
ALTER TABLE HR.tbl_TimeRecoveryPlans ADD CONSTRAINT PK_TimeRecoveryPlans PRIMARY KEY (RecoveryPlanID);
ALTER TABLE HR.tbl_TimeRecoveryLogs ADD CONSTRAINT PK_TimeRecoveryLogs PRIMARY KEY (RecoveryLogID);

-- Tablas de planificación de tiempo
ALTER TABLE HR.tbl_TimePlanning ADD CONSTRAINT PK_TimePlanning PRIMARY KEY (PlanID);
ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT PK_TimePlanningEmployees PRIMARY KEY (PlanEmployeeID);
ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT PK_TimePlanningExecution PRIMARY KEY (ExecutionID);

-- Tablas de movimientos y nómina
ALTER TABLE HR.tbl_Subrogations ADD CONSTRAINT PK_Subrogations PRIMARY KEY (SubrogationID);
ALTER TABLE HR.tbl_PersonnelMovements ADD CONSTRAINT PK_PersonnelMovements PRIMARY KEY (MovementID);
ALTER TABLE HR.tbl_Payroll ADD CONSTRAINT PK_Payroll PRIMARY KEY (PayrollID);
ALTER TABLE HR.tbl_PayrollLines ADD CONSTRAINT PK_PayrollLines PRIMARY KEY (PayrollLineID);
ALTER TABLE HR.tbl_SalaryHistory ADD CONSTRAINT PK_SalaryHistory PRIMARY KEY (SalaryHistoryID);

-- Tabla de auditoría
ALTER TABLE HR.tbl_Audit ADD CONSTRAINT PK_Audit PRIMARY KEY (AuditID);

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

 

-- =============================================
-- 2.2 UNIQUE CONSTRAINTS
-- =============================================
PRINT '2.2. Agregando UNIQUE CONSTRAINTS...';


ALTER TABLE HR.ref_Types ADD CONSTRAINT UQ_ref_Types_CategoryName UNIQUE (Category, Name);
ALTER TABLE HR.tbl_People ADD CONSTRAINT UQ_People_IDCard UNIQUE (IDCard);
ALTER TABLE HR.tbl_People ADD CONSTRAINT UQ_People_Email UNIQUE (Email);
ALTER TABLE HR.tbl_Departments ADD CONSTRAINT UQ_Departments_Code UNIQUE (Code);
ALTER TABLE HR.tbl_Employees ADD CONSTRAINT UQ_Employees_Email UNIQUE (Email);
--ALTER TABLE HR.tbl_Payroll ADD CONSTRAINT UQ_Payroll_EmployeePeriod UNIQUE (EmployeeID, Period);
ALTER TABLE HR.tbl_contract_type ADD CONSTRAINT UQ_contract_type_ContractCode UNIQUE (ContractCode);
-- Unique constraint para feriados (misma fecha no puede repetirse)
CREATE UNIQUE INDEX UX_Holidays_Date ON HR.tbl_Holidays(HolidayDate) WHERE IsActive = 1;

-- =============================================
-- 2.3 CHECK CONSTRAINTS
-- =============================================
PRINT '2.3. Agregando CHECK CONSTRAINTS...';

-- Check constraints para tbl_AttendancePunches
ALTER TABLE HR.tbl_AttendancePunches 
ADD CONSTRAINT CK_AttendancePunches_PunchType CHECK (PunchType IN ('In','Out'));

-- Check constraints para tbl_Vacations
ALTER TABLE HR.tbl_Vacations 
ADD CONSTRAINT CK_Vacations_Status CHECK (Status IN ('Planned','InProgress','Completed','Canceled'));

-- Check constraints para tbl_Permissions
ALTER TABLE HR.tbl_Permissions 
ADD CONSTRAINT CK_Permissions_Status CHECK (Status IN ('Pending','Approved','Rejected'));

-- Check constraints para tbl_PunchJustifications
ALTER TABLE HR.tbl_PunchJustifications 
ADD CONSTRAINT CK_PunchJustifications_Status CHECK (Status IN ('PENDING','APPROVED','REJECTED'));

-- Check constraints para tbl_AttendanceCalculations
ALTER TABLE HR.tbl_AttendanceCalculations 
ADD CONSTRAINT CK_AttendanceCalculations_Status CHECK (Status IN ('Pending','Approved'));

-- Check constraints para tbl_Overtime
ALTER TABLE HR.tbl_Overtime 
ADD CONSTRAINT CK_Overtime_Status CHECK (Status IN ('Planned','Verified','Rejected','Paid'));

-- Check constraints para tbl_Payroll
ALTER TABLE HR.tbl_Payroll 
ADD CONSTRAINT CK_Payroll_Status CHECK (Status IN ('Pending','Paid','Reconciled'));

-- Check constraints para tbl_PayrollLines
ALTER TABLE HR.tbl_PayrollLines 
ADD CONSTRAINT CK_PayrollLines_LineType CHECK (LineType IN ('Earning','Deduction','Subsidy','Overtime'));

-- Check constraints para tbl_PersonnelMovements
ALTER TABLE HR.tbl_PersonnelMovements 
ADD CONSTRAINT CK_PersonnelMovements_MovementType CHECK (MovementType IN ('Transfer','Promotion','Demotion','Lateral'));

-- Check constraints para tbl_TimePlanning
ALTER TABLE HR.tbl_TimePlanning 
ADD CONSTRAINT CK_TimePlanning_PlanType CHECK (PlanType IN ('Overtime','Recovery'));

-- Check constraints para fechas
ALTER TABLE HR.tbl_Subrogations 
ADD CONSTRAINT CK_Subrogations_Dates CHECK (EndDate >= StartDate);

ALTER TABLE HR.tbl_Contracts 
ADD CONSTRAINT CK_Contracts_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate);

ALTER TABLE HR.tbl_Vacations 
ADD CONSTRAINT CK_Vacations_Dates CHECK (EndDate >= StartDate);

ALTER TABLE HR.tbl_Permissions 
ADD CONSTRAINT CK_Permissions_Dates CHECK (EndDate >= StartDate);

ALTER TABLE HR.tbl_Trainings 
ADD CONSTRAINT CK_Trainings_Dates CHECK (EndDate >= StartDate);

ALTER TABLE HR.tbl_WorkExperiences 
ADD CONSTRAINT CK_WorkExperiences_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate);

-- Check constraints para valores positivos
ALTER TABLE HR.tbl_Overtime 
ADD CONSTRAINT CK_Overtime_Hours CHECK (Hours > 0);

--ALTER TABLE HR.tbl_Contracts 
--ADD CONSTRAINT CK_Contracts_BaseSalary CHECK (BaseSalary >= 0);

ALTER TABLE HR.tbl_Payroll 
ADD CONSTRAINT CK_Payroll_BaseSalary CHECK (BaseSalary >= 0);

ALTER TABLE HR.tbl_SalaryHistory 
ADD CONSTRAINT CK_SalaryHistory_Salaries CHECK (OldSalary >= 0 AND NewSalary >= 0);

-- =============================================
-- 2.4 DEFAULT CONSTRAINTS
-- =============================================
PRINT '2.4. Agregando DEFAULT CONSTRAINTS...';
/*
-- Default para campos de estado activo
ALTER TABLE HR.tbl_People ADD CONSTRAINT DF_People_IsActive DEFAULT (1) FOR IsActive;
ALTER TABLE HR.tbl_Departments ADD CONSTRAINT DF_Departments_IsActive DEFAULT (1) FOR IsActive;
ALTER TABLE HR.tbl_Employees ADD CONSTRAINT DF_Employees_IsActive DEFAULT (1) FOR IsActive;
ALTER TABLE HR.tbl_jobs ADD CONSTRAINT DF_jobs_IsActive DEFAULT (1) FOR IsActive;
ALTER TABLE HR.tbl_PersonnelMovements ADD CONSTRAINT DF_PersonnelMovements_IsActive DEFAULT (1) FOR IsActive;
ALTER TABLE HR.tbl_Holidays ADD CONSTRAINT DF_Holidays_IsActive DEFAULT (1) FOR IsActive;

-- Default para campos de fecha
ALTER TABLE HR.tbl_Permissions ADD CONSTRAINT DF_Permissions_RequestDate DEFAULT (GETDATE()) FOR RequestDate;
ALTER TABLE HR.tbl_AttendancePunches ADD CONSTRAINT DF_AttendancePunches_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;
ALTER TABLE HR.tbl_PunchJustifications ADD CONSTRAINT DF_PunchJustifications_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;
ALTER TABLE HR.tbl_Overtime ADD CONSTRAINT DF_Overtime_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;

-- Default para campos de estado
ALTER TABLE HR.tbl_Vacations ADD CONSTRAINT DF_Vacations_Status DEFAULT ('Planned') FOR Status;
ALTER TABLE HR.tbl_Permissions ADD CONSTRAINT DF_Permissions_Status DEFAULT ('Pending') FOR Status;
ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT DF_AttendanceCalculations_Status DEFAULT ('Pending') FOR Status;
ALTER TABLE HR.tbl_Overtime ADD CONSTRAINT DF_Overtime_Status DEFAULT ('Planned') FOR Status;
ALTER TABLE HR.tbl_Payroll ADD CONSTRAINT DF_Payroll_Status DEFAULT ('Pending') FOR Status;
ALTER TABLE HR.tbl_PunchJustifications ADD CONSTRAINT DF_PunchJustifications_Status DEFAULT ('PENDING') FOR Status;

-- Default para campos numéricos
ALTER TABLE HR.tbl_Vacations ADD CONSTRAINT DF_Vacations_DaysTaken DEFAULT (0) FOR DaysTaken;
ALTER TABLE HR.tbl_Permissions ADD CONSTRAINT DF_Permissions_ChargedToVacation DEFAULT (0) FOR ChargedToVacation;
ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT DF_AttendanceCalculations_TotalWorkedMinutes DEFAULT (0) FOR TotalWorkedMinutes;
ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT DF_AttendanceCalculations_RegularMinutes DEFAULT (0) FOR RegularMinutes;
ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT DF_AttendanceCalculations_OvertimeMinutes DEFAULT (0) FOR OvertimeMinutes;
ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT DF_AttendanceCalculations_NightMinutes DEFAULT (0) FOR NightMinutes;
ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT DF_AttendanceCalculations_HolidayMinutes DEFAULT (0) FOR HolidayMinutes;
ALTER TABLE HR.tbl_Overtime ADD CONSTRAINT DF_Overtime_ActualHours DEFAULT (0) FOR ActualHours;
ALTER TABLE HR.tbl_Overtime ADD CONSTRAINT DF_Overtime_PaymentAmount DEFAULT (0) FOR PaymentAmount;
ALTER TABLE HR.tbl_PayrollLines ADD CONSTRAINT DF_PayrollLines_Quantity DEFAULT (1) FOR Quantity;
ALTER TABLE HR.tbl_PayrollLines ADD CONSTRAINT DF_PayrollLines_UnitValue DEFAULT (0) FOR UnitValue;

-- Default para campos booleanos de configuración
ALTER TABLE HR.tbl_Schedules ADD CONSTRAINT DF_Schedules_HasLunchBreak DEFAULT (1) FOR HasLunchBreak;
ALTER TABLE HR.tbl_Schedules ADD CONSTRAINT DF_Schedules_IsRotating DEFAULT (0) FOR IsRotating;
ALTER TABLE HR.tbl_PermissionTypes ADD CONSTRAINT DF_PermissionTypes_DeductsFromVacation DEFAULT (0) FOR DeductsFromVacation;
ALTER TABLE HR.tbl_PermissionTypes ADD CONSTRAINT DF_PermissionTypes_RequiresApproval DEFAULT (1) FOR RequiresApproval;
ALTER TABLE HR.tbl_TimePlanning ADD CONSTRAINT DF_TimePlanning_RequiresApproval DEFAULT (1) FOR RequiresApproval;
ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT DF_TimePlanningEmployees_IsEligible DEFAULT (1) FOR IsEligible;
ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT DF_TimePlanningEmployees_ActualHours DEFAULT (0) FOR ActualHours;
ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT DF_TimePlanningEmployees_ActualMinutes DEFAULT (0) FOR ActualMinutes;
ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT DF_TimePlanningEmployees_PaymentAmount DEFAULT (0) FOR PaymentAmount;
ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT DF_TimePlanningExecution_TotalMinutes DEFAULT (0) FOR TotalMinutes;
ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT DF_TimePlanningExecution_RegularMinutes DEFAULT (0) FOR RegularMinutes;
ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT DF_TimePlanningExecution_OvertimeMinutes DEFAULT (0) FOR OvertimeMinutes;
ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT DF_TimePlanningExecution_NightMinutes DEFAULT (0) FOR NightMinutes;
ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT DF_TimePlanningExecution_HolidayMinutes DEFAULT (0) FOR HolidayMinutes;
ALTER TABLE HR.tbl_PunchJustifications ADD CONSTRAINT DF_PunchJustifications_Approved DEFAULT (0) FOR Approved;
ALTER TABLE HR.tbl_WorkExperiences ADD CONSTRAINT DF_WorkExperiences_IsCurrent DEFAULT (0) FOR IsCurrent;
*/
-- =============================================
-- 2.5 FOREIGN KEYS (EN ORDEN CORRECTO)
-- =============================================
PRINT '2.5. Agregando FOREIGN KEYS...';

-- 1. FOREIGN KEYS PARA TABLAS GEOGRÁFICAS (PRIMERO - SIN DEPENDENCIAS)
ALTER TABLE HR.tbl_Provinces
ADD CONSTRAINT FK_Provinces_Country FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID);

ALTER TABLE HR.tbl_Cantons
ADD CONSTRAINT FK_Cantons_Province FOREIGN KEY (ProvinceID) REFERENCES HR.tbl_Provinces(ProvinceID);

-- 2. FOREIGN KEYS PARA TABLA DE PERSONAS (DEPENDE DE GEOGRÁFICAS Y REF_TYPES)
ALTER TABLE HR.tbl_People
ADD CONSTRAINT FK_People_IdentType FOREIGN KEY (IdentType) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_Sex FOREIGN KEY (Sex) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_Gender FOREIGN KEY (Gender) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_MaritalStatus FOREIGN KEY (MaritalStatusTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_Ethnicity FOREIGN KEY (EthnicityTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_BloodType FOREIGN KEY (BloodTypeTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_SpecialNeeds FOREIGN KEY (SpecialNeedsTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_Country FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID),
    CONSTRAINT FK_People_Province FOREIGN KEY (ProvinceID) REFERENCES HR.tbl_Provinces(ProvinceID),
    CONSTRAINT FK_People_Canton FOREIGN KEY (CantonID) REFERENCES HR.tbl_Cantons(CantonID);

-- 3. FOREIGN KEYS PARA TABLAS DE CLASIFICACIÓN LABORAL
ALTER TABLE HR.tbl_Occupational_Groups 
ADD CONSTRAINT FK_Occupational_Groups_Degrees FOREIGN KEY (DegreeID) REFERENCES HR.tbl_Degrees(DegreeID);

ALTER TABLE HR.tbl_jobs
ADD CONSTRAINT FK_jobs_Occupational_Groups FOREIGN KEY (GroupID) REFERENCES HR.tbl_Occupational_Groups(GroupID),
    CONSTRAINT FK_jobs_JobType FOREIGN KEY (JobTypeID) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_JobActivities
ADD CONSTRAINT FK_JobActivities_Activities FOREIGN KEY (ActivitiesID) REFERENCES HR.tbl_Activities(ActivitiesID),
    CONSTRAINT FK_JobActivities_Jobs FOREIGN KEY (tbl_jobs) REFERENCES HR.tbl_jobs(JobID);

ALTER TABLE HR.tbl_contract_type
ADD CONSTRAINT FK_contract_type_PersonalContractType FOREIGN KEY (PersonalContractTypeID) REFERENCES HR.ref_Types(TypeID);

-- 4. FOREIGN KEYS PARA TABLA DE DEPARTAMENTOS (AUTO-REFERENCIAL)
ALTER TABLE HR.tbl_Departments
ADD CONSTRAINT FK_Departments_Parent FOREIGN KEY (ParentID) REFERENCES HR.tbl_Departments(DepartmentID),
    CONSTRAINT FK_Departments_DepartmentType FOREIGN KEY (DepartmentType) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Departments_DeanDirector FOREIGN KEY (DeanDirector) REFERENCES HR.tbl_Employees(EmployeeID);

-- 5. FOREIGN KEYS PARA TABLA DE EMPLEADOS (DEPENDE DE PERSONAS, DEPARTAMENTOS Y REF_TYPES)
ALTER TABLE HR.tbl_Employees
ADD CONSTRAINT FK_Employees_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Employees_Department FOREIGN KEY (DepartmentID) REFERENCES HR.tbl_Departments(DepartmentID),
    CONSTRAINT FK_Employees_EmployeeType FOREIGN KEY (EmployeeType) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Employees_Boss FOREIGN KEY (ImmediateBossID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Employees_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Employees_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees(EmployeeID);

-- 6. FOREIGN KEYS PARA TABLAS DE SOLICITUDES Y CERTIFICACIONES
ALTER TABLE HR.tbl_contractRequest
ADD CONSTRAINT FK_contractRequest_WorkModality FOREIGN KEY (WorkModalityID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_contractRequest_Status FOREIGN KEY (Status) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_FinancialCertification
ADD CONSTRAINT FK_FinancialCertification_Request FOREIGN KEY (RequestID) REFERENCES HR.tbl_contractRequest(RequestID),
    CONSTRAINT FK_FinancialCertification_Status FOREIGN KEY (Status) REFERENCES HR.ref_Types(TypeID);

-- 7. FOREIGN KEYS PARA TABLAS DE CONTRATOS
ALTER TABLE HR.tbl_Contracts
ADD CONSTRAINT FK_Contracts_Certification FOREIGN KEY (CertificationID) REFERENCES HR.tbl_FinancialCertification(CertificationID),
    CONSTRAINT FK_Contracts_Parent FOREIGN KEY (ParentID) REFERENCES HR.tbl_Contracts(ContractID),
    CONSTRAINT FK_Contracts_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Contracts_ContractType FOREIGN KEY (ContractTypeID) REFERENCES HR.tbl_contract_type(ContractTypeID),
    CONSTRAINT FK_Contracts_Job FOREIGN KEY (JobID) REFERENCES HR.tbl_jobs(JobID),
    CONSTRAINT FK_Contracts_Department FOREIGN KEY (DepartmentID) REFERENCES HR.tbl_Departments(DepartmentID),
    CONSTRAINT FK_Contracts_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Contracts_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_AdditionalActivities
ADD CONSTRAINT FK_AdditionalActivities_Activities FOREIGN KEY (ActivitiesID) REFERENCES HR.tbl_Activities(ActivitiesID),
    CONSTRAINT FK_AdditionalActivities_Contract FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts(ContractID);

ALTER TABLE HR.tbl_contractAttachedFile
ADD CONSTRAINT FK_contractAttachedFile_Contract FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts(ContractID),
    CONSTRAINT FK_contractAttachedFile_Type FOREIGN KEY (typeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_contractAttachedFile_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID);

-- 8. FOREIGN KEYS PARA TABLAS OPERATIVAS PRINCIPALES
ALTER TABLE HR.tbl_EmployeeSchedules
ADD CONSTRAINT FK_EmployeeSchedules_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_EmployeeSchedules_Schedule FOREIGN KEY (ScheduleID) REFERENCES HR.tbl_Schedules(ScheduleID);

ALTER TABLE HR.tbl_Vacations
ADD CONSTRAINT FK_Vacations_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_Permissions
ADD CONSTRAINT FK_Permissions_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Permissions_PermissionType FOREIGN KEY (PermissionTypeID) REFERENCES HR.tbl_PermissionTypes(TypeID),
    CONSTRAINT FK_Permissions_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Permissions_Vacation FOREIGN KEY (VacationID) REFERENCES HR.tbl_Vacations(VacationID);

-- 9. FOREIGN KEYS PARA TABLAS DE ASISTENCIA
ALTER TABLE HR.tbl_AttendancePunches
ADD CONSTRAINT FK_AttendancePunches_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_PunchJustifications
ADD CONSTRAINT FK_PunchJustifications_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_PunchJustifications_Boss FOREIGN KEY (BossEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_PunchJustifications_JustificationType FOREIGN KEY (JustificationTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_PunchJustifications_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_AttendanceCalculations
ADD CONSTRAINT FK_AttendanceCalculations_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

-- 10. FOREIGN KEYS PARA TABLAS DE HORAS EXTRA Y RECUPERACIÓN
ALTER TABLE HR.tbl_Overtime
ADD CONSTRAINT FK_Overtime_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Overtime_OvertimeType FOREIGN KEY (OvertimeType) REFERENCES HR.tbl_OvertimeConfig(OvertimeType),
    CONSTRAINT FK_Overtime_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Overtime_SecondApprover FOREIGN KEY (SecondApprover) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_TimeRecoveryPlans
ADD CONSTRAINT FK_TimeRecoveryPlans_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_TimeRecoveryPlans_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_TimeRecoveryLogs
ADD CONSTRAINT FK_TimeRecoveryLogs_Plan FOREIGN KEY (RecoveryPlanID) REFERENCES HR.tbl_TimeRecoveryPlans(RecoveryPlanID),
    CONSTRAINT FK_TimeRecoveryLogs_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES HR.tbl_Employees(EmployeeID);

-- 11. FOREIGN KEYS PARA TABLAS DE PLANIFICACIÓN DE TIEMPO
ALTER TABLE HR.tbl_TimePlanning
ADD CONSTRAINT FK_TimePlanning_PlanStatus FOREIGN KEY (PlanStatusTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_TimePlanning_OvertimeType FOREIGN KEY (OvertimeType) REFERENCES HR.tbl_OvertimeConfig(OvertimeType),
    CONSTRAINT FK_TimePlanning_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_TimePlanning_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_TimePlanning_SecondApprover FOREIGN KEY (SecondApprover) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_TimePlanning_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_TimePlanningEmployees
ADD CONSTRAINT FK_TimePlanningEmployees_Plan FOREIGN KEY (PlanID) REFERENCES HR.tbl_TimePlanning(PlanID) ON DELETE CASCADE,
    CONSTRAINT FK_TimePlanningEmployees_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_TimePlanningEmployees_EmployeeStatus FOREIGN KEY (EmployeeStatusTypeID) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_TimePlanningExecution
ADD CONSTRAINT FK_TimePlanningExecution_PlanEmployee FOREIGN KEY (PlanEmployeeID) REFERENCES HR.tbl_TimePlanningEmployees(PlanEmployeeID),
    CONSTRAINT FK_TimePlanningExecution_VerifiedBy FOREIGN KEY (VerifiedBy) REFERENCES HR.tbl_Employees(EmployeeID);

-- 12. FOREIGN KEYS PARA TABLAS DE MOVIMIENTOS Y SUBROGACIONES
ALTER TABLE HR.tbl_Subrogations
ADD CONSTRAINT FK_Subrogations_SubrogatedEmployee FOREIGN KEY (SubrogatedEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Subrogations_SubrogatingEmployee FOREIGN KEY (SubrogatingEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_Subrogations_Permission FOREIGN KEY (PermissionID) REFERENCES HR.tbl_Permissions(PermissionID),
    CONSTRAINT FK_Subrogations_Vacation FOREIGN KEY (VacationID) REFERENCES HR.tbl_Vacations(VacationID);

ALTER TABLE HR.tbl_PersonnelMovements
ADD CONSTRAINT FK_PersonnelMovements_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
    CONSTRAINT FK_PersonnelMovements_Contract FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts(ContractID),
    CONSTRAINT FK_PersonnelMovements_Job FOREIGN KEY (JobID) REFERENCES HR.tbl_jobs(JobID),
    CONSTRAINT FK_PersonnelMovements_OriginDepartment FOREIGN KEY (OriginDepartmentID) REFERENCES HR.tbl_Departments(DepartmentID),
    CONSTRAINT FK_PersonnelMovements_DestinationDepartment FOREIGN KEY (DestinationDepartmentID) REFERENCES HR.tbl_Departments(DepartmentID),
    CONSTRAINT FK_PersonnelMovements_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID);

-- 13. FOREIGN KEYS PARA TABLAS DE NÓMINA
ALTER TABLE HR.tbl_Payroll
ADD CONSTRAINT FK_Payroll_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

ALTER TABLE HR.tbl_PayrollLines
ADD CONSTRAINT FK_PayrollLines_Payroll FOREIGN KEY (PayrollID) REFERENCES HR.tbl_Payroll(PayrollID) ON DELETE CASCADE;

ALTER TABLE HR.tbl_SalaryHistory
ADD CONSTRAINT FK_SalaryHistory_Contract FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts(ContractID);

-- 14. FOREIGN KEYS PARA TABLAS DE HISTORIA DE VIDA
ALTER TABLE HR.tbl_Addresses
ADD CONSTRAINT FK_Addresses_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Addresses_AddressType FOREIGN KEY (AddressTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Addresses_Country FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID),
    CONSTRAINT FK_Addresses_Province FOREIGN KEY (ProvinceID) REFERENCES HR.tbl_Provinces(ProvinceID),
    CONSTRAINT FK_Addresses_Canton FOREIGN KEY (CantonID) REFERENCES HR.tbl_Cantons(CantonID);

ALTER TABLE HR.tbl_Institutions
ADD CONSTRAINT FK_Institutions_InstitutionType FOREIGN KEY (InstitutionTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Institutions_Country FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID),
    CONSTRAINT FK_Institutions_Province FOREIGN KEY (ProvinceID) REFERENCES HR.tbl_Provinces(ProvinceID),
    CONSTRAINT FK_Institutions_Canton FOREIGN KEY (CantonID) REFERENCES HR.tbl_Cantons(CantonID);

ALTER TABLE HR.tbl_EducationLevels
ADD CONSTRAINT FK_EducationLevels_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_EducationLevels_EducationLevelType FOREIGN KEY (EducationLevelTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_EducationLevels_Institution FOREIGN KEY (InstitutionID) REFERENCES HR.tbl_Institutions(InstitutionID);

ALTER TABLE HR.tbl_EmergencyContacts
ADD CONSTRAINT FK_EmergencyContacts_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_EmergencyContacts_RelationshipType FOREIGN KEY (RelationshipTypeID) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_CatastrophicIllnesses
ADD CONSTRAINT FK_CatastrophicIllnesses_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_CatastrophicIllnesses_IllnessType FOREIGN KEY (IllnessTypeID) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_FamilyBurden
ADD CONSTRAINT FK_FamilyBurden_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_FamilyBurden_IdentificationType FOREIGN KEY (IdentificationTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_FamilyBurden_DisabilityType FOREIGN KEY (DisabilityTypeID) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_Trainings
ADD CONSTRAINT FK_Trainings_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Trainings_KnowledgeArea FOREIGN KEY (KnowledgeAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Trainings_EventType FOREIGN KEY (EventTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Trainings_CertificateType FOREIGN KEY (CertificateTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Trainings_ApprovalType FOREIGN KEY (ApprovalTypeID) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_WorkExperiences
ADD CONSTRAINT FK_WorkExperiences_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_WorkExperiences_InstitutionType FOREIGN KEY (InstitutionTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_WorkExperiences_ExperienceType FOREIGN KEY (ExperienceTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_WorkExperiences_Country FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID);

ALTER TABLE HR.tbl_BankAccounts
ADD CONSTRAINT FK_BankAccounts_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_BankAccounts_AccountType FOREIGN KEY (AccountTypeID) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_Publications
ADD CONSTRAINT FK_Publications_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Publications_PublicationType FOREIGN KEY (PublicationTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Publications_JournalType FOREIGN KEY (JournalTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Publications_KnowledgeArea FOREIGN KEY (KnowledgeAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Publications_SubArea FOREIGN KEY (SubAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Publications_Area FOREIGN KEY (AreaTypeID) REFERENCES HR.ref_Types(TypeID);

ALTER TABLE HR.tbl_Books
ADD CONSTRAINT FK_Books_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Books_Country FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID),
    CONSTRAINT FK_Books_KnowledgeArea FOREIGN KEY (KnowledgeAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Books_SubArea FOREIGN KEY (SubAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Books_Area FOREIGN KEY (AreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Books_ParticipationType FOREIGN KEY (ParticipationTypeID) REFERENCES HR.ref_Types(TypeID);

--ALTER TABLE HR.contract_type ADD CONSTRAINT UQ_contract_type_ContractCode UNIQUE (ContractCode);


PRINT 'TODOS LOS CONSTRAINTS CREADOS EXITOSAMENTE.';
GO