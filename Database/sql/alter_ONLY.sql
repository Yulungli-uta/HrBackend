-- ALTER extraídos
GO

ALTER TABLE HR.tbl_Holidays ADD CONSTRAINT PK_Holidays PRIMARY KEY (HolidayID);

-- 4.2 Primary Key para tbl_TimePlanning
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanning')
GO

ALTER TABLE HR.tbl_TimePlanning ADD CONSTRAINT PK_TimePlanning PRIMARY KEY (PlanID);

-- 4.3 Primary Key para tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanningEmployees')
GO

ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT PK_TimePlanningEmployees PRIMARY KEY (PlanEmployeeID);

-- 4.4 Primary Key para tbl_TimePlanningExecution
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanningExecution')
GO

ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT PK_TimePlanningExecution PRIMARY KEY (ExecutionID);

PRINT 'Primary Keys agregados exitosamente';
GO

ALTER TABLE HR.tbl_TimePlanning ADD CONSTRAINT CK_TimePlanning_PlanType 
    CHECK (PlanType IN ('Overtime','Recovery'));

-- 5.2 Check constraints para tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanningEmployees_Hours')
GO

ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT CK_TimePlanningEmployees_Hours 
    CHECK (AssignedHours >= 0 OR AssignedHours IS NULL);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanningEmployees_Minutes')
GO

ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT CK_TimePlanningEmployees_Minutes 
    CHECK (AssignedMinutes >= 0 OR AssignedMinutes IS NULL);

-- 5.3 Unique constraint para feriados (misma fecha no puede repetirse)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Holidays_Date' AND object_id = OBJECT_ID('HR.tbl_Holidays'))
GO

ALTER TABLE HR.tbl_TimePlanningEmployees 
    ADD CONSTRAINT FK_TimePlanningEmployees_Plan 
    FOREIGN KEY (PlanID) REFERENCES HR.tbl_TimePlanning(PlanID) ON DELETE CASCADE;

-- 7.2 FK para tbl_TimePlanningEmployees → tbl_Employees
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningEmployees_Employee')
GO

ALTER TABLE HR.tbl_TimePlanningEmployees 
    ADD CONSTRAINT FK_TimePlanningEmployees_Employee 
    FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);

-- 7.3 FK para tbl_TimePlanningEmployees → ref_Types (Estado del empleado)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningEmployees_Status')
GO

ALTER TABLE HR.tbl_TimePlanningEmployees 
    ADD CONSTRAINT FK_TimePlanningEmployees_Status 
    FOREIGN KEY (EmployeeStatusTypeID) REFERENCES HR.ref_Types(TypeID);

-- 7.4 FK para tbl_TimePlanningExecution → tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningExecution_PlanEmployee')
GO

ALTER TABLE HR.tbl_TimePlanningExecution 
    ADD CONSTRAINT FK_TimePlanningExecution_PlanEmployee 
    FOREIGN KEY (PlanEmployeeID) REFERENCES HR.tbl_TimePlanningEmployees(PlanEmployeeID);

-- 7.5 FK para tbl_TimePlanning → ref_Types (Estado de la planificación)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_PlanStatus')
GO

ALTER TABLE HR.tbl_TimePlanning 
    ADD CONSTRAINT FK_TimePlanning_PlanStatus 
    FOREIGN KEY (PlanStatusTypeID) REFERENCES HR.ref_Types(TypeID);

-- 7.6 FK para tbl_TimePlanning → tbl_OvertimeConfig (Tipo de hora extra)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_OvertimeType')
GO

ALTER TABLE HR.tbl_TimePlanning 
    ADD CONSTRAINT FK_TimePlanning_OvertimeType 
    FOREIGN KEY (OvertimeType) REFERENCES HR.tbl_OvertimeConfig(OvertimeType);

-- 7.7 FK para campos de auditoría en tbl_TimePlanning
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_CreatedBy')
GO

ALTER TABLE HR.tbl_TimePlanning 
    ADD CONSTRAINT FK_TimePlanning_CreatedBy 
    FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_ApprovedBy')
GO

ALTER TABLE HR.tbl_TimePlanning 
    ADD CONSTRAINT FK_TimePlanning_ApprovedBy 
    FOREIGN KEY (ApprovedBy) REFERENCES HR.tbl_Employees(EmployeeID);

-- 7.8 FK para tbl_TimePlanningExecution → tbl_Employees (VerifiedBy)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningExecution_VerifiedBy')
GO

ALTER TABLE HR.tbl_TimePlanningExecution 
    ADD CONSTRAINT FK_TimePlanningExecution_VerifiedBy 
    FOREIGN KEY (VerifiedBy) REFERENCES HR.tbl_Employees(EmployeeID);

PRINT 'Foreign Keys agregados exitosamente';
GO

ALTER TABLE dbutasystem.HR.tbl_PersonnelMovements
ADD IsActive BIT NOT NULL DEFAULT(1);

-------------------------------------------------------------------------------
-- 2) ALTERs: PK, UNIQUE y FKs
-------------------------------------------------------------------------------

-- PKs
-- Tabla de tipos
GO

ALTER TABLE HR.ref_Types ADD CONSTRAINT PK_ref_Types PRIMARY KEY (TypeID);
GO

ALTER TABLE HR.tbl_People              ADD CONSTRAINT PK_People PRIMARY KEY (PersonID);
--ALTER TABLE HR.tbl_Faculties           ADD CONSTRAINT PK_Faculties PRIMARY KEY (FacultyID);
GO

ALTER TABLE HR.tbl_Departments         ADD CONSTRAINT PK_Departments PRIMARY KEY (DepartmentID);
GO

ALTER TABLE HR.tbl_Employees           ADD CONSTRAINT PK_Employees PRIMARY KEY (EmployeeID);
GO

ALTER TABLE HR.tbl_Schedules           ADD CONSTRAINT PK_Schedules PRIMARY KEY (ScheduleID);
GO

ALTER TABLE HR.tbl_EmployeeSchedules   ADD CONSTRAINT PK_EmployeeSchedules PRIMARY KEY (EmpScheduleID);
GO

ALTER TABLE HR.tbl_Contracts           ADD CONSTRAINT PK_Contracts PRIMARY KEY (ContractID);
GO

ALTER TABLE HR.tbl_SalaryHistory       ADD CONSTRAINT PK_SalaryHistory PRIMARY KEY (SalaryHistoryID);
GO

ALTER TABLE HR.tbl_PermissionTypes     ADD CONSTRAINT PK_PermissionTypes PRIMARY KEY (TypeID);
GO

ALTER TABLE HR.tbl_Vacations           ADD CONSTRAINT PK_Vacations PRIMARY KEY (VacationID);
GO

ALTER TABLE HR.tbl_Permissions         ADD CONSTRAINT PK_Permissions PRIMARY KEY (PermissionID);
GO

ALTER TABLE HR.tbl_AttendancePunches   ADD CONSTRAINT PK_AttendancePunches PRIMARY KEY (PunchID);
GO

ALTER TABLE HR.tbl_PunchJustifications ADD CONSTRAINT PK_PunchJustifications PRIMARY KEY (PunchJustID);
GO

ALTER TABLE HR.tbl_AttendanceCalculations ADD CONSTRAINT PK_AttendanceCalculations PRIMARY KEY (CalculationID);
GO

ALTER TABLE HR.tbl_OvertimeConfig      ADD CONSTRAINT PK_OvertimeConfig PRIMARY KEY (OvertimeType);
GO

ALTER TABLE HR.tbl_Overtime            ADD CONSTRAINT PK_Overtime PRIMARY KEY (OvertimeID);
GO

ALTER TABLE HR.tbl_TimeRecoveryPlans   ADD CONSTRAINT PK_TimeRecoveryPlans PRIMARY KEY (RecoveryPlanID);
GO

ALTER TABLE HR.tbl_TimeRecoveryLogs    ADD CONSTRAINT PK_TimeRecoveryLogs PRIMARY KEY (RecoveryLogID);
GO

ALTER TABLE HR.tbl_Subrogations        ADD CONSTRAINT PK_Subrogations PRIMARY KEY (SubrogationID);
GO

ALTER TABLE HR.tbl_PersonnelMovements  ADD CONSTRAINT PK_PersonnelMovements PRIMARY KEY (MovementID);
GO

ALTER TABLE HR.tbl_Payroll             ADD CONSTRAINT PK_Payroll PRIMARY KEY (PayrollID);
GO

ALTER TABLE HR.tbl_PayrollLines        ADD CONSTRAINT PK_PayrollLines PRIMARY KEY (PayrollLineID);
GO

ALTER TABLE HR.tbl_Audit               ADD CONSTRAINT PK_Audit PRIMARY KEY (AuditID);
--ALTER TABLE HR.tbl_Departments  	  ADD CONSTRAINT PK_Departments PRIMARY KEY (DepartmentID);
GO

ALTER TABLE HR.tbl_Departments 		   ADD CONSTRAINT UK_Departments_Code UNIQUE (Code);




-- UNIQUEs
GO

ALTER TABLE HR.tbl_People      ADD CONSTRAINT UQ_People_IDCard UNIQUE(IDCard);
GO

ALTER TABLE HR.tbl_People      ADD CONSTRAINT UQ_People_Email  UNIQUE(Email);
--ALTER TABLE HR.tbl_Departments ADD CONSTRAINT UQ_Department UNIQUE(FacultyID, Name);
GO

ALTER TABLE HR.tbl_Payroll     ADD CONSTRAINT UQ_Payroll UNIQUE(EmployeeID, Period);

-- FKs
--ALTER TABLE HR.tbl_Departments
  --  ADD CONSTRAINT FK_Departments_Faculty
    --    FOREIGN KEY (FacultyID) REFERENCES HR.tbl_Faculties(FacultyID);

--ALTER TABLE HR.tbl_Faculties
  --  ADD CONSTRAINT FK_Faculties_Dean FOREIGN KEY (DeanEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_Departments 
	ADD CONSTRAINT FK_Departments_Parent FOREIGN KEY (ParentID) REFERENCES HR.tbl_Departments(DepartmentID);
GO

ALTER TABLE HR.tbl_Employees
    ADD CONSTRAINT FK_Employees_Persons FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_People(PersonID) ON DELETE CASCADE;
GO

ALTER TABLE HR.tbl_Employees
    ADD CONSTRAINT FK_Employees_Department FOREIGN KEY (DepartmentID) REFERENCES HR.tbl_Departments(DepartmentID);
GO

ALTER TABLE HR.tbl_Employees
    ADD CONSTRAINT FK_Employees_Boss FOREIGN KEY (ImmediateBossID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_EmployeeSchedules
    ADD CONSTRAINT FK_EmpSchedules_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_EmpSchedules_Schedule FOREIGN KEY (ScheduleID) REFERENCES HR.tbl_Schedules(ScheduleID);
GO

ALTER TABLE HR.tbl_Contracts
    ADD CONSTRAINT FK_Contracts_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_SalaryHistory
    ADD CONSTRAINT FK_SalaryHistory_Contract FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts(ContractID);
GO

ALTER TABLE HR.tbl_Vacations
    ADD CONSTRAINT FK_Vacations_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_Permissions
    ADD CONSTRAINT FK_Permissions_Employee  FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_Permissions_Type      FOREIGN KEY (PermissionTypeID) REFERENCES HR.tbl_PermissionTypes(TypeID),
        CONSTRAINT FK_Permissions_Vacation  FOREIGN KEY (VacationID) REFERENCES HR.tbl_Vacations(VacationID);
GO

ALTER TABLE HR.tbl_AttendancePunches
    ADD CONSTRAINT FK_Punch_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_PunchJustifications
    ADD --CONSTRAINT FK_PJ_Punch  FOREIGN KEY (PunchID) REFERENCES HR.tbl_AttendancePunches(PunchID) ON DELETE CASCADE,
        CONSTRAINT FK_PJ_Boss   FOREIGN KEY (BossEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_AttendanceCalculations
    ADD CONSTRAINT FK_AttCalc_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_Overtime
    ADD CONSTRAINT FK_OT_Employee    FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_OT_Type        FOREIGN KEY (OvertimeType) REFERENCES HR.tbl_OvertimeConfig(OvertimeType);
GO

ALTER TABLE HR.tbl_TimeRecoveryPlans
    ADD CONSTRAINT FK_TRP_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_TimeRecoveryLogs
    ADD CONSTRAINT FK_TRL_Plan FOREIGN KEY (RecoveryPlanID) REFERENCES HR.tbl_TimeRecoveryPlans(RecoveryPlanID);
GO

ALTER TABLE HR.tbl_Subrogations
    ADD CONSTRAINT FK_Subro_Titular  FOREIGN KEY (SubrogatedEmployeeID)  REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_Subro_Subroga  FOREIGN KEY (SubrogatingEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_Subro_Perm     FOREIGN KEY (PermissionID) REFERENCES HR.tbl_Permissions(PermissionID),
        CONSTRAINT FK_Subro_Vac      FOREIGN KEY (VacationID)   REFERENCES HR.tbl_Vacations(VacationID);
GO

ALTER TABLE HR.tbl_PersonnelMovements
    ADD CONSTRAINT FK_Move_Employee  FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_Move_Contract  FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts(ContractID),
        CONSTRAINT FK_Move_Origin    FOREIGN KEY (OriginDepartmentID) REFERENCES HR.tbl_Departments(DepartmentID),
        CONSTRAINT FK_Move_Dest      FOREIGN KEY (DestinationDepartmentID) REFERENCES HR.tbl_Departments(DepartmentID);
GO

ALTER TABLE HR.tbl_Payroll
    ADD CONSTRAINT FK_Payroll_Employee FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO

ALTER TABLE HR.tbl_PayrollLines
    ADD CONSTRAINT FK_PL_Payroll FOREIGN KEY (PayrollID) REFERENCES HR.tbl_Payroll(PayrollID) ON DELETE CASCADE;

-- Checks adicionales
GO

ALTER TABLE HR.tbl_TimeRecoveryPlans
    ADD CONSTRAINT CK_TRP_AtLeastPlannedMinutes CHECK (DATEDIFF(MINUTE, FromTime, ToTime) >= OwedMinutes AND DATEDIFF(MINUTE, FromTime, ToTime) >= 60);
GO

ALTER TABLE HR.tbl_Subrogations
    ADD CONSTRAINT CK_Subro_Dates CHECK (EndDate >= StartDate);
	
	/*HOJA DE VIDA */
GO

ALTER TABLE [HR].[tbl_AttendancePunches]
GO

ALTER COLUMN [CreatedAt] datetime2;
GO

ALTER TABLE [HR].[tbl_AttendancePunches]
GO

ALTER COLUMN [PunchTime] datetime2;
	
	--ALTER TABLE [HR].[tbl_Employees] 
	--ALTER COLUMN [type] INT;
	
	IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningEmployees_Status')
GO

ALTER TABLE HR.tbl_Departments 
    ADD CONSTRAINT FK_Departments_RefType
    FOREIGN KEY (DepartmentType) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE [HR].[tbl_Employees]
    ADD CONSTRAINT FK_Employees_type  FOREIGN KEY (type)  REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE dbutasystem.[HR].[tbl_contracts]
    ADD CONSTRAINT FK_contract_type  FOREIGN KEY (contractType)  REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE dbutasystem.HR.tbl_PersonnelMovements
	ADD CONSTRAINT FK_person_Job  FOREIGN KEY (JobID)  REFERENCES HR.tbl_jobs(JobID);
/*IF OBJECT_ID('HR.ref_Types','U') IS NOT NULL DROP TABLE HR.ref_Types;
GO

ALTER TABLE [HR].[tbl_PunchJustifications] 
ADD CONSTRAINT [FK_PunchJustifications_TypeID] 
FOREIGN KEY ([JustificationTypeID]) REFERENCES [HR].[ref_Types] ([TypeID])
GO

ALTER TABLE [HR].[tbl_PunchJustifications] 
ADD CONSTRAINT [CK_Status] 
CHECK ([Status] IN ('PENDING', 'APPROVED', 'REJECTED'))
GO

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
GO

ALTER TABLE HR.tbl_Countries ADD CONSTRAINT PK_Countries PRIMARY KEY (CountryID);
GO

ALTER TABLE HR.tbl_Provinces ADD CONSTRAINT PK_Provinces PRIMARY KEY (ProvinceID);
GO

ALTER TABLE HR.tbl_Cantons ADD CONSTRAINT PK_Cantons PRIMARY KEY (CantonID);
GO

ALTER TABLE HR.tbl_Addresses ADD CONSTRAINT PK_Addresses PRIMARY KEY (AddressID);
GO

ALTER TABLE HR.tbl_Institutions ADD CONSTRAINT PK_Institutions PRIMARY KEY (InstitutionID);
GO

ALTER TABLE HR.tbl_EducationLevels ADD CONSTRAINT PK_EducationLevels PRIMARY KEY (EducationID);
GO

ALTER TABLE HR.tbl_EmergencyContacts ADD CONSTRAINT PK_EmergencyContacts PRIMARY KEY (ContactID);
GO

ALTER TABLE HR.tbl_CatastrophicIllnesses ADD CONSTRAINT PK_CatastrophicIllnesses PRIMARY KEY (IllnessID);
GO

ALTER TABLE HR.tbl_FamilyBurden ADD CONSTRAINT PK_FamilyBurden PRIMARY KEY (BurdenID);
GO

ALTER TABLE HR.tbl_Trainings ADD CONSTRAINT PK_Trainings PRIMARY KEY (TrainingID);
GO

ALTER TABLE HR.tbl_WorkExperiences ADD CONSTRAINT PK_WorkExperiences PRIMARY KEY (WorkExpID);
GO

ALTER TABLE HR.tbl_BankAccounts ADD CONSTRAINT PK_BankAccounts PRIMARY KEY (AccountID);
GO

ALTER TABLE HR.tbl_Publications ADD CONSTRAINT PK_Publications PRIMARY KEY (PublicationID);
GO

ALTER TABLE HR.tbl_Books ADD CONSTRAINT PK_Books PRIMARY KEY (BookID);
GO

ALTER TABLE HR.ref_Types ADD CONSTRAINT UQ_TypeCategoryName UNIQUE (Category, Name);
-- Removidas las restricciones únicas de CountryCode, ProvinceCode, CantonCode ya que estos campos no existen
GO

ALTER TABLE HR.tbl_Provinces
ADD CONSTRAINT FK_Provinces_Country 
    FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID);
GO

ALTER TABLE HR.tbl_Cantons
ADD CONSTRAINT FK_Cantons_Province 
    FOREIGN KEY (ProvinceID) REFERENCES HR.tbl_Provinces(ProvinceID);
GO

ALTER TABLE HR.tbl_People
ADD CONSTRAINT FK_People_Country 
    FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID),
    CONSTRAINT FK_People_Province 
    FOREIGN KEY (ProvinceID) REFERENCES HR.tbl_Provinces(ProvinceID),
    CONSTRAINT FK_People_Canton 
    FOREIGN KEY (CantonID) REFERENCES HR.tbl_Cantons(CantonID),
    CONSTRAINT FK_People_MaritalStatusType 
    FOREIGN KEY (MaritalStatusTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_EthnicityType 
    FOREIGN KEY (EthnicityTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_BloodType 
    FOREIGN KEY (BloodTypeTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_People_SpecialNeedsType 
    FOREIGN KEY (SpecialNeedsTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_Addresses
ADD CONSTRAINT FK_Addresses_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Addresses_Country 
    FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID),
    CONSTRAINT FK_Addresses_Province 
    FOREIGN KEY (ProvinceID) REFERENCES HR.tbl_Provinces(ProvinceID),
    CONSTRAINT FK_Addresses_Canton 
    FOREIGN KEY (CantonID) REFERENCES HR.tbl_Cantons(CantonID),
    CONSTRAINT FK_Addresses_AddressType 
    FOREIGN KEY (AddressTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_Institutions
ADD CONSTRAINT FK_Institutions_Type 
    FOREIGN KEY (InstitutionTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Institutions_Country 
    FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID),
    CONSTRAINT FK_Institutions_Province 
    FOREIGN KEY (ProvinceID) REFERENCES HR.tbl_Provinces(ProvinceID),
    CONSTRAINT FK_Institutions_Canton 
    FOREIGN KEY (CantonID) REFERENCES HR.tbl_Cantons(CantonID);
GO

ALTER TABLE HR.tbl_EducationLevels
ADD CONSTRAINT FK_EducationLevels_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_EducationLevels_EducationType 
    FOREIGN KEY (EducationLevelTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_EducationLevels_Institution 
    FOREIGN KEY (InstitutionID) REFERENCES HR.tbl_Institutions(InstitutionID);
GO

ALTER TABLE HR.tbl_EmergencyContacts
ADD CONSTRAINT FK_EmergencyContacts_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_EmergencyContacts_Relationship 
    FOREIGN KEY (RelationshipTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_CatastrophicIllnesses
ADD CONSTRAINT FK_CatastrophicIllnesses_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_CatastrophicIllnesses_Type 
    FOREIGN KEY (IllnessTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_FamilyBurden
ADD CONSTRAINT FK_FamilyBurden_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_FamilyBurden_IDType 
    FOREIGN KEY (IdentificationTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_FamilyBurden_DisabilityType 
    FOREIGN KEY (DisabilityTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_Trainings
ADD CONSTRAINT FK_Trainings_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Trainings_KnowledgeArea 
    FOREIGN KEY (KnowledgeAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Trainings_EventType 
    FOREIGN KEY (EventTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Trainings_CertificateType 
    FOREIGN KEY (CertificateTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Trainings_ApprovalType 
    FOREIGN KEY (ApprovalTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_WorkExperiences
ADD CONSTRAINT FK_WorkExperiences_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_WorkExperiences_InstitutionType 
    FOREIGN KEY (InstitutionTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_WorkExperiences_ExperienceType 
    FOREIGN KEY (ExperienceTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_WorkExperiences_Country 
    FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID);
GO

ALTER TABLE HR.tbl_BankAccounts
ADD CONSTRAINT FK_BankAccounts_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_BankAccounts_AccountType 
    FOREIGN KEY (AccountTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_Publications
ADD CONSTRAINT FK_Publications_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Publications_PublicationType 
    FOREIGN KEY (PublicationTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Publications_JournalType 
    FOREIGN KEY (JournalTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Publications_KnowledgeArea 
    FOREIGN KEY (KnowledgeAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Publications_SubArea 
    FOREIGN KEY (SubAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Publications_Area 
    FOREIGN KEY (AreaTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_Books
ADD CONSTRAINT FK_Books_Person 
    FOREIGN KEY (PersonID) REFERENCES HR.tbl_People(PersonID),
    CONSTRAINT FK_Books_Country 
    FOREIGN KEY (CountryID) REFERENCES HR.tbl_Countries(CountryID),
    CONSTRAINT FK_Books_KnowledgeArea 
    FOREIGN KEY (KnowledgeAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Books_SubArea 
    FOREIGN KEY (SubAreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Books_Area 
    FOREIGN KEY (AreaTypeID) REFERENCES HR.ref_Types(TypeID),
    CONSTRAINT FK_Books_ParticipationType 
    FOREIGN KEY (ParticipationTypeID) REFERENCES HR.ref_Types(TypeID);
GO

ALTER TABLE HR.tbl_Trainings
ADD CONSTRAINT CK_Trainings_Dates CHECK (EndDate >= StartDate);
GO

ALTER TABLE HR.tbl_WorkExperiences
ADD CONSTRAINT CK_WorkExp_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate);
GO

