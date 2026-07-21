-- ============================================================
-- CONSTRAINTS (PK + UNIQUE + FK): esquema [HR]
-- Orden garantizado: PKs primero, FKs en orden topológico de dependencias
-- Generado: 2026-05-29
-- ============================================================

SET NOCOUNT ON;
GO

-- ============================================================
-- BLOQUE 1: PRIMARY KEYS
-- (Deben existir antes de crear cualquier FK que las referencie)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ref_Types')
    ALTER TABLE [HR].[ref_Types]
        ADD CONSTRAINT [PK_ref_Types] PRIMARY KEY CLUSTERED ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_stg_EmployeeScheduleLoad')
    ALTER TABLE [HR].[stg_EmployeeScheduleLoad]
        ADD CONSTRAINT [PK_stg_EmployeeScheduleLoad] PRIMARY KEY CLUSTERED ([StagingID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_AcademicLadder')
    ALTER TABLE [HR].[tbl_AcademicLadder]
        ADD CONSTRAINT [PK_AcademicLadder] PRIMARY KEY CLUSTERED ([LadderID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Activities')
    ALTER TABLE [HR].[tbl_Activities]
        ADD CONSTRAINT [PK_Activities] PRIMARY KEY CLUSTERED ([ActivitiesID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_AdditionalActivities')
    ALTER TABLE [HR].[tbl_AdditionalActivities]
        ADD CONSTRAINT [PK_AdditionalActivities] PRIMARY KEY CLUSTERED ([ActivitiesID], [ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Addresses')
    ALTER TABLE [HR].[tbl_Addresses]
        ADD CONSTRAINT [PK_Addresses] PRIMARY KEY CLUSTERED ([AddressID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__tbl_Atte__5E5499A821B25944')
    ALTER TABLE [HR].[tbl_AttendanceCalcLog]
        ADD CONSTRAINT [PK__tbl_Atte__5E5499A821B25944] PRIMARY KEY CLUSTERED ([LogID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_AttendanceCalculations')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [PK_AttendanceCalculations] PRIMARY KEY CLUSTERED ([CalculationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_AttendancePunches')
    ALTER TABLE [HR].[tbl_AttendancePunches]
        ADD CONSTRAINT [PK_AttendancePunches] PRIMARY KEY CLUSTERED ([PunchID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Audit')
    ALTER TABLE [HR].[tbl_Audit]
        ADD CONSTRAINT [PK_Audit] PRIMARY KEY CLUSTERED ([AuditID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_BankAccounts')
    ALTER TABLE [HR].[tbl_BankAccounts]
        ADD CONSTRAINT [PK_BankAccounts] PRIMARY KEY CLUSTERED ([AccountID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Books')
    ALTER TABLE [HR].[tbl_Books]
        ADD CONSTRAINT [PK_Books] PRIMARY KEY CLUSTERED ([BookID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Cantons')
    ALTER TABLE [HR].[tbl_Cantons]
        ADD CONSTRAINT [PK_Cantons] PRIMARY KEY CLUSTERED ([CantonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_CatastrophicIllnesses')
    ALTER TABLE [HR].[tbl_CatastrophicIllnesses]
        ADD CONSTRAINT [PK_CatastrophicIllnesses] PRIMARY KEY CLUSTERED ([IllnessID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__tbl_cont__4D7B4ADDF6A67CCA')
    ALTER TABLE [HR].[tbl_contract_status_history]
        ADD CONSTRAINT [PK__tbl_cont__4D7B4ADDF6A67CCA] PRIMARY KEY CLUSTERED ([HistoryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__tbl_cont__54F04847D04F1892')
    ALTER TABLE [HR].[tbl_contract_status_transitions]
        ADD CONSTRAINT [PK__tbl_cont__54F04847D04F1892] PRIMARY KEY CLUSTERED ([TransitionID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_contract_type')
    ALTER TABLE [HR].[tbl_contract_type]
        ADD CONSTRAINT [PK_contract_type] PRIMARY KEY CLUSTERED ([ContractTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_contractRequest')
    ALTER TABLE [HR].[tbl_contractRequest]
        ADD CONSTRAINT [PK_contractRequest] PRIMARY KEY CLUSTERED ([RequestID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_contractRequestPerson')
    ALTER TABLE [HR].[tbl_contractRequestPerson]
        ADD CONSTRAINT [PK_contractRequestPerson] PRIMARY KEY CLUSTERED ([RequestPersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Contracts')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED ([ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Countries')
    ALTER TABLE [HR].[tbl_Countries]
        ADD CONSTRAINT [PK_Countries] PRIMARY KEY CLUSTERED ([CountryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Degrees')
    ALTER TABLE [HR].[tbl_Degrees]
        ADD CONSTRAINT [PK_Degrees] PRIMARY KEY CLUSTERED ([DegreeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_DeptAuth')
    ALTER TABLE [HR].[tbl_DepartmentAuthorities]
        ADD CONSTRAINT [PK_DeptAuth] PRIMARY KEY CLUSTERED ([AuthorityID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Departments')
    ALTER TABLE [HR].[tbl_Departments]
        ADD CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__TBL_Dire__3D93EF02D28EF112')
    ALTER TABLE [HR].[TBL_DirectoryParameters]
        ADD CONSTRAINT [PK__TBL_Dire__3D93EF02D28EF112] PRIMARY KEY CLUSTERED ([DirectoryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_DocumentTemplateFields')
    ALTER TABLE [HR].[tbl_DocumentTemplateFields]
        ADD CONSTRAINT [PK_DocumentTemplateFields] PRIMARY KEY CLUSTERED ([FieldID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_DocumentTemplates')
    ALTER TABLE [HR].[tbl_DocumentTemplates]
        ADD CONSTRAINT [PK_DocumentTemplates] PRIMARY KEY CLUSTERED ([TemplateID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EducationLevels')
    ALTER TABLE [HR].[tbl_EducationLevels]
        ADD CONSTRAINT [PK_EducationLevels] PRIMARY KEY CLUSTERED ([EducationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_tbl_EmailLayouts')
    ALTER TABLE [HR].[tbl_EmailLayouts]
        ADD CONSTRAINT [PK_tbl_EmailLayouts] PRIMARY KEY CLUSTERED ([EmailLayoutID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_tbl_EmailLogAttachments')
    ALTER TABLE [HR].[tbl_EmailLogAttachments]
        ADD CONSTRAINT [PK_tbl_EmailLogAttachments] PRIMARY KEY CLUSTERED ([EmailLogAttachmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_tbl_EmailLogs')
    ALTER TABLE [HR].[tbl_EmailLogs]
        ADD CONSTRAINT [PK_tbl_EmailLogs] PRIMARY KEY CLUSTERED ([EmailLogID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EmergencyContacts')
    ALTER TABLE [HR].[tbl_EmergencyContacts]
        ADD CONSTRAINT [PK_EmergencyContacts] PRIMARY KEY CLUSTERED ([ContactID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EmployeeAvailabilityBlocks')
    ALTER TABLE [HR].[tbl_EmployeeAvailabilityBlocks]
        ADD CONSTRAINT [PK_EmployeeAvailabilityBlocks] PRIMARY KEY CLUSTERED ([BlockID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Employees')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EmployeeSchedules')
    ALTER TABLE [HR].[tbl_EmployeeSchedules]
        ADD CONSTRAINT [PK_EmployeeSchedules] PRIMARY KEY CLUSTERED ([EmpScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_FamilyBurden')
    ALTER TABLE [HR].[tbl_FamilyBurden]
        ADD CONSTRAINT [PK_FamilyBurden] PRIMARY KEY CLUSTERED ([BurdenID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_FinancialCertification')
    ALTER TABLE [HR].[tbl_FinancialCertification]
        ADD CONSTRAINT [PK_FinancialCertification] PRIMARY KEY CLUSTERED ([CertificationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_FinancialCertificationRejectionHistory')
    ALTER TABLE [HR].[tbl_FinancialCertificationRejectionHistory]
        ADD CONSTRAINT [PK_FinancialCertificationRejectionHistory] PRIMARY KEY CLUSTERED ([RejectionHistoryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GeneratedDocumentFields')
    ALTER TABLE [HR].[tbl_GeneratedDocumentFields]
        ADD CONSTRAINT [PK_GeneratedDocumentFields] PRIMARY KEY CLUSTERED ([DocumentFieldID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GeneratedDocuments')
    ALTER TABLE [HR].[tbl_GeneratedDocuments]
        ADD CONSTRAINT [PK_GeneratedDocuments] PRIMARY KEY CLUSTERED ([DocumentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardAssignmentValidations')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [PK_GuardAssignmentValidations] PRIMARY KEY CLUSTERED ([ValidationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardEmployeeSpecialRules')
    ALTER TABLE [HR].[tbl_GuardEmployeeSpecialRules]
        ADD CONSTRAINT [PK_GuardEmployeeSpecialRules] PRIMARY KEY CLUSTERED ([SpecialRuleId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardGroupRotationPatterns')
    ALTER TABLE [HR].[tbl_GuardGroupRotationPatterns]
        ADD CONSTRAINT [PK_GuardGroupRotationPatterns] PRIMARY KEY CLUSTERED ([GroupPatternID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_LocationRotationAssignments')
    ALTER TABLE [HR].[tbl_GuardLocationRotationAssignments]
        ADD CONSTRAINT [PK_LocationRotationAssignments] PRIMARY KEY CLUSTERED ([LocationRotationAssignmentId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_LocationRotationPeriods')
    ALTER TABLE [HR].[tbl_GuardLocationRotationPeriods]
        ADD CONSTRAINT [PK_LocationRotationPeriods] PRIMARY KEY CLUSTERED ([LocationRotationPeriodId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardRotationGroupEmployees')
    ALTER TABLE [HR].[tbl_GuardRotationGroupEmployees]
        ADD CONSTRAINT [PK_GuardRotationGroupEmployees] PRIMARY KEY CLUSTERED ([GroupEmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardRotationGroups')
    ALTER TABLE [HR].[tbl_GuardRotationGroups]
        ADD CONSTRAINT [PK_GuardRotationGroups] PRIMARY KEY CLUSTERED ([GroupID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardServiceLocations')
    ALTER TABLE [HR].[tbl_GuardServiceLocations]
        ADD CONSTRAINT [PK_GuardServiceLocations] PRIMARY KEY CLUSTERED ([LocationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardSettings')
    ALTER TABLE [HR].[tbl_GuardSettings]
        ADD CONSTRAINT [PK_GuardSettings] PRIMARY KEY CLUSTERED ([SettingKey]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardShiftChanges')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [PK_GuardShiftChanges] PRIMARY KEY CLUSTERED ([ShiftChangeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardShiftCoverageRequirements')
    ALTER TABLE [HR].[tbl_GuardShiftCoverageRequirements]
        ADD CONSTRAINT [PK_GuardShiftCoverageRequirements] PRIMARY KEY CLUSTERED ([RequirementID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardShiftPlanning')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [PK_GuardShiftPlanning] PRIMARY KEY CLUSTERED ([PlanningID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardVacationPlans')
    ALTER TABLE [HR].[tbl_GuardVacationPlans]
        ADD CONSTRAINT [PK_GuardVacationPlans] PRIMARY KEY CLUSTERED ([GuardVacationPlanId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GuardVacationRequests')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [PK_GuardVacationRequests] PRIMARY KEY CLUSTERED ([GuardVacationRequestId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Holidays')
    ALTER TABLE [HR].[tbl_Holidays]
        ADD CONSTRAINT [PK_Holidays] PRIMARY KEY CLUSTERED ([HolidayID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Institutions')
    ALTER TABLE [HR].[tbl_Institutions]
        ADD CONSTRAINT [PK_Institutions] PRIMARY KEY CLUSTERED ([InstitutionID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_JobActivities')
    ALTER TABLE [HR].[tbl_JobActivities]
        ADD CONSTRAINT [PK_JobActivities] PRIMARY KEY CLUSTERED ([ActivitiesID], [JobID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_jobs')
    ALTER TABLE [HR].[tbl_jobs]
        ADD CONSTRAINT [PK_jobs] PRIMARY KEY CLUSTERED ([JobID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__tbl_Know__3213E83F4D43D69F')
    ALTER TABLE [HR].[tbl_KnowledgeArea]
        ADD CONSTRAINT [PK__tbl_Know__3213E83F4D43D69F] PRIMARY KEY CLUSTERED ([id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Occupational_Groups')
    ALTER TABLE [HR].[tbl_Occupational_Groups]
        ADD CONSTRAINT [PK_Occupational_Groups] PRIMARY KEY CLUSTERED ([GroupID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Overtime')
    ALTER TABLE [HR].[tbl_Overtime]
        ADD CONSTRAINT [PK_Overtime] PRIMARY KEY CLUSTERED ([OvertimeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_OvertimeConfig')
    ALTER TABLE [HR].[tbl_OvertimeConfig]
        ADD CONSTRAINT [PK_OvertimeConfig] PRIMARY KEY CLUSTERED ([OvertimeType]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__TBL_PARA__F80C6297540B98CC')
    ALTER TABLE [HR].[TBL_PARAMETERS]
        ADD CONSTRAINT [PK__TBL_PARA__F80C6297540B98CC] PRIMARY KEY CLUSTERED ([ParameterID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Payroll')
    ALTER TABLE [HR].[tbl_Payroll]
        ADD CONSTRAINT [PK_Payroll] PRIMARY KEY CLUSTERED ([PayrollID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PayrollLines')
    ALTER TABLE [HR].[tbl_PayrollLines]
        ADD CONSTRAINT [PK_PayrollLines] PRIMARY KEY CLUSTERED ([PayrollLineID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_People')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [PK_People] PRIMARY KEY CLUSTERED ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Permissions')
    ALTER TABLE [HR].[tbl_Permissions]
        ADD CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([PermissionID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PermissionTypes')
    ALTER TABLE [HR].[tbl_PermissionTypes]
        ADD CONSTRAINT [PK_PermissionTypes] PRIMARY KEY CLUSTERED ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_tbl_personnel_action_type')
    ALTER TABLE [HR].[tbl_personnel_action_type]
        ADD CONSTRAINT [PK_tbl_personnel_action_type] PRIMARY KEY CLUSTERED ([PersonnelActionTypeId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PersonnelActions')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [PK_PersonnelActions] PRIMARY KEY CLUSTERED ([ActionID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PersonnelActionStatusHistory')
    ALTER TABLE [HR].[tbl_PersonnelActionStatusHistory]
        ADD CONSTRAINT [PK_PersonnelActionStatusHistory] PRIMARY KEY CLUSTERED ([HistoryId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PersonnelMovements')
    ALTER TABLE [HR].[tbl_PersonnelMovements]
        ADD CONSTRAINT [PK_PersonnelMovements] PRIMARY KEY CLUSTERED ([MovementID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Provinces')
    ALTER TABLE [HR].[tbl_Provinces]
        ADD CONSTRAINT [PK_Provinces] PRIMARY KEY CLUSTERED ([ProvinceID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Publications')
    ALTER TABLE [HR].[tbl_Publications]
        ADD CONSTRAINT [PK_Publications] PRIMARY KEY CLUSTERED ([PublicationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PunchJustifications')
    ALTER TABLE [HR].[tbl_PunchJustifications]
        ADD CONSTRAINT [PK_PunchJustifications] PRIMARY KEY CLUSTERED ([PunchJustID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__tbl_Repo__3214EC073A027545')
    ALTER TABLE [HR].[tbl_ReportAudit]
        ADD CONSTRAINT [PK__tbl_Repo__3214EC073A027545] PRIMARY KEY CLUSTERED ([Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_RotationPatternDetails')
    ALTER TABLE [HR].[tbl_RotationPatternDetails]
        ADD CONSTRAINT [PK_RotationPatternDetails] PRIMARY KEY CLUSTERED ([PatternDetailID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_RotationPatterns')
    ALTER TABLE [HR].[tbl_RotationPatterns]
        ADD CONSTRAINT [PK_RotationPatterns] PRIMARY KEY CLUSTERED ([PatternID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_SalaryHistory')
    ALTER TABLE [HR].[tbl_SalaryHistory]
        ADD CONSTRAINT [PK_SalaryHistory] PRIMARY KEY CLUSTERED ([SalaryHistoryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ScheduleChangePlan')
    ALTER TABLE [HR].[tbl_ScheduleChangePlan]
        ADD CONSTRAINT [PK_ScheduleChangePlan] PRIMARY KEY CLUSTERED ([PlanID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ScheduleChangePlanDetail')
    ALTER TABLE [HR].[tbl_ScheduleChangePlanDetail]
        ADD CONSTRAINT [PK_ScheduleChangePlanDetail] PRIMARY KEY CLUSTERED ([DetailID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Schedules')
    ALTER TABLE [HR].[tbl_Schedules]
        ADD CONSTRAINT [PK_Schedules] PRIMARY KEY CLUSTERED ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__TBL_Stor__6F0F98BF8B80C2AF')
    ALTER TABLE [HR].[TBL_StoredFile]
        ADD CONSTRAINT [PK__TBL_Stor__6F0F98BF8B80C2AF] PRIMARY KEY CLUSTERED ([FileId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Subrogations')
    ALTER TABLE [HR].[tbl_Subrogations]
        ADD CONSTRAINT [PK_Subrogations] PRIMARY KEY CLUSTERED ([SubrogationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TeacherStructure')
    ALTER TABLE [HR].[tbl_TeacherStructure]
        ADD CONSTRAINT [PK_TeacherStructure] PRIMARY KEY CLUSTERED ([TeacherStructureID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__tbl_Time__D1822466B9467EFC')
    ALTER TABLE [HR].[tbl_TimeBalanceMovements]
        ADD CONSTRAINT [PK__tbl_Time__D1822466B9467EFC] PRIMARY KEY CLUSTERED ([MovementID]);
GO

-- 2026-07-06 (Fase 3, propuesta multi-régimen): PK cambiada de (EmployeeID) a
-- (EmployeeID, LaborRegimeId) — un saldo por régimen activo, no uno solo por
-- empleado. Ejecutado y verificado en producción (674 -> 676 filas).
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimeBalances_Employee_Regime')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK__tbl_Time__7AD04FF13895AF94')
        ALTER TABLE [HR].[tbl_TimeBalances] DROP CONSTRAINT [PK__tbl_Time__7AD04FF13895AF94];

    ALTER TABLE [HR].[tbl_TimeBalances]
        ADD CONSTRAINT [PK_TimeBalances_Employee_Regime] PRIMARY KEY CLUSTERED ([EmployeeID], [LaborRegimeId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanning')
    ALTER TABLE [HR].[tbl_TimePlanning]
        ADD CONSTRAINT [PK_TimePlanning] PRIMARY KEY CLUSTERED ([PlanID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanningEmployees')
    ALTER TABLE [HR].[tbl_TimePlanningEmployees]
        ADD CONSTRAINT [PK_TimePlanningEmployees] PRIMARY KEY CLUSTERED ([PlanEmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanningExecution')
    ALTER TABLE [HR].[tbl_TimePlanningExecution]
        ADD CONSTRAINT [PK_TimePlanningExecution] PRIMARY KEY CLUSTERED ([ExecutionID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimeRecoveryLogs')
    ALTER TABLE [HR].[tbl_TimeRecoveryLogs]
        ADD CONSTRAINT [PK_TimeRecoveryLogs] PRIMARY KEY CLUSTERED ([RecoveryLogID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimeRecoveryPlans')
    ALTER TABLE [HR].[tbl_TimeRecoveryPlans]
        ADD CONSTRAINT [PK_TimeRecoveryPlans] PRIMARY KEY CLUSTERED ([RecoveryPlanID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Languages')
    ALTER TABLE [HR].[tbl_Languages]
        ADD CONSTRAINT [PK_Languages] PRIMARY KEY CLUSTERED ([LanguageID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Trainings')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [PK_Trainings] PRIMARY KEY CLUSTERED ([TrainingID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Vacations')
    ALTER TABLE [HR].[tbl_Vacations]
        ADD CONSTRAINT [PK_Vacations] PRIMARY KEY CLUSTERED ([VacationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_WorkExperiences')
    ALTER TABLE [HR].[tbl_WorkExperiences]
        ADD CONSTRAINT [PK_WorkExperiences] PRIMARY KEY CLUSTERED ([WorkExpID]);
GO

-- ============================================================
-- BLOQUE 2: UNIQUE CONSTRAINTS
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_Types_CategoryName')
    ALTER TABLE [HR].[ref_Types]
        ADD CONSTRAINT [UQ_ref_Types_CategoryName] UNIQUE ([Category], [Name]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_AcadLadder_Code')
    ALTER TABLE [HR].[tbl_AcademicLadder]
        ADD CONSTRAINT [UQ_AcadLadder_Code] UNIQUE ([Code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_contract_status_transitions')
    ALTER TABLE [HR].[tbl_contract_status_transitions]
        ADD CONSTRAINT [UQ_contract_status_transitions] UNIQUE ([FromStatusTypeID], [ToStatusTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Departments_Code')
    ALTER TABLE [HR].[tbl_Departments]
        ADD CONSTRAINT [UQ_Departments_Code] UNIQUE ([Code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ__TBL_Dire__A25C5AA7C4CA07C2')
    ALTER TABLE [HR].[TBL_DirectoryParameters]
        ADD CONSTRAINT [UQ__TBL_Dire__A25C5AA7C4CA07C2] UNIQUE ([Code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_DocumentTemplateFields_TemplateFieldName')
    ALTER TABLE [HR].[tbl_DocumentTemplateFields]
        ADD CONSTRAINT [UQ_DocumentTemplateFields_TemplateFieldName] UNIQUE ([TemplateID], [FieldName]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Employees_Email')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [UQ_Employees_Email] UNIQUE ([Email]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ__tbl_Know__357D4CF91A3C8FE6')
    ALTER TABLE [HR].[tbl_KnowledgeArea]
        ADD CONSTRAINT [UQ__tbl_Know__357D4CF91A3C8FE6] UNIQUE ([code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ__TBL_PARA__72E12F1B31F929C1')
    ALTER TABLE [HR].[TBL_PARAMETERS]
        ADD CONSTRAINT [UQ__TBL_PARA__72E12F1B31F929C1] UNIQUE ([name]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_People_Email')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [UQ_People_Email] UNIQUE ([Email]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_People_IDCard')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [UQ_People_IDCard] UNIQUE ([IDCard]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UX_tbl_personnel_action_type_Code')
    ALTER TABLE [HR].[tbl_personnel_action_type]
        ADD CONSTRAINT [UX_tbl_personnel_action_type_Code] UNIQUE ([Code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_RotationPatternDetails_PatternDay')
    ALTER TABLE [HR].[tbl_RotationPatternDetails]
        ADD CONSTRAINT [UQ_RotationPatternDetails_PatternDay] UNIQUE ([PatternID], [DayOrder]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_SCPD_PlanEmployee')
    ALTER TABLE [HR].[tbl_ScheduleChangePlanDetail]
        ADD CONSTRAINT [UQ_SCPD_PlanEmployee] UNIQUE ([PlanID], [EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_TeacherStr_ActiveEmployee')
    ALTER TABLE [HR].[tbl_TeacherStructure]
        ADD CONSTRAINT [UQ_TeacherStr_ActiveEmployee] UNIQUE ([EmployeeID], [StartDate]);
GO

-- 2026-07-06: cierra el hueco que permitió insertar el mismo plan/log de
-- recuperación varias veces (encontrado con datos reales duplicados).
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_TimeRecoveryPlans_Employee_Date_Range')
    ALTER TABLE [HR].[tbl_TimeRecoveryPlans]
        ADD CONSTRAINT [UQ_TimeRecoveryPlans_Employee_Date_Range] UNIQUE ([EmployeeID], [PlanDate], [FromTime], [ToTime]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_TimeRecoveryLogs_Plan_ExecutedDate')
    ALTER TABLE [HR].[tbl_TimeRecoveryLogs]
        ADD CONSTRAINT [UQ_TimeRecoveryLogs_Plan_ExecutedDate] UNIQUE ([RecoveryPlanID], [ExecutedDate]);
GO

-- ============================================================
-- BLOQUE 3: FOREIGN KEYS
-- (Ordenadas topológicamente: tablas independientes primero)
-- ============================================================

-- --- Tabla: tbl_Departments ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_InstitutionalRoleType')
    ALTER TABLE [HR].[tbl_Departments]
        ADD CONSTRAINT [FK_Departments_InstitutionalRoleType]
            FOREIGN KEY ([InstitutionalRoleTypeId])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_contract_status_transitions ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contract_status_transitions_to')
    ALTER TABLE [HR].[tbl_contract_status_transitions]
        ADD CONSTRAINT [FK_contract_status_transitions_to]
            FOREIGN KEY ([ToStatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contract_status_transitions_from')
    ALTER TABLE [HR].[tbl_contract_status_transitions]
        ADD CONSTRAINT [FK_contract_status_transitions_from]
            FOREIGN KEY ([FromStatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_contractRequest ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contractRequest_Status')
    ALTER TABLE [HR].[tbl_contractRequest]
        ADD CONSTRAINT [FK_contractRequest_Status]
            FOREIGN KEY ([Status])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contractRequest_WorkModality')
    ALTER TABLE [HR].[tbl_contractRequest]
        ADD CONSTRAINT [FK_contractRequest_WorkModality]
            FOREIGN KEY ([WorkModalityID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_Provinces ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Provinces_Country')
    ALTER TABLE [HR].[tbl_Provinces]
        ADD CONSTRAINT [FK_Provinces_Country]
            FOREIGN KEY ([CountryID])
            REFERENCES [HR].[tbl_Countries] ([CountryID]);
GO

-- --- Tabla: tbl_Occupational_Groups ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Occupational_Groups_Degrees')
    ALTER TABLE [HR].[tbl_Occupational_Groups]
        ADD CONSTRAINT [FK_Occupational_Groups_Degrees]
            FOREIGN KEY ([DegreeID])
            REFERENCES [HR].[tbl_Degrees] ([DegreeID]);
GO

-- --- Tabla: TBL_StoredFile ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TBL_StoredFile_DirectoryCode')
    ALTER TABLE [HR].[TBL_StoredFile]
        ADD CONSTRAINT [FK_TBL_StoredFile_DirectoryCode]
            FOREIGN KEY ([DirectoryCode])
            REFERENCES [HR].[TBL_DirectoryParameters] ([Code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StoredFile_DocumentTypeId')
    ALTER TABLE [HR].[TBL_StoredFile]
        ADD CONSTRAINT [FK_StoredFile_DocumentTypeId]
            FOREIGN KEY ([DocumentTypeId])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_FinancialCertification ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinancialCertification_RejectionType')
    ALTER TABLE [HR].[tbl_FinancialCertification]
        ADD CONSTRAINT [FK_FinancialCertification_RejectionType]
            FOREIGN KEY ([RejectionTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinancialCertification_Status')
    ALTER TABLE [HR].[tbl_FinancialCertification]
        ADD CONSTRAINT [FK_FinancialCertification_Status]
            FOREIGN KEY ([Status])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinancialCertification_Request')
    ALTER TABLE [HR].[tbl_FinancialCertification]
        ADD CONSTRAINT [FK_FinancialCertification_Request]
            FOREIGN KEY ([RequestID])
            REFERENCES [HR].[tbl_contractRequest] ([RequestID]);
GO

-- --- Tabla: tbl_Cantons ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Cantons_Province')
    ALTER TABLE [HR].[tbl_Cantons]
        ADD CONSTRAINT [FK_Cantons_Province]
            FOREIGN KEY ([ProvinceID])
            REFERENCES [HR].[tbl_Provinces] ([ProvinceID]);
GO

-- --- Tabla: tbl_jobs ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_jobs_Occupational_Groups')
    ALTER TABLE [HR].[tbl_jobs]
        ADD CONSTRAINT [FK_jobs_Occupational_Groups]
            FOREIGN KEY ([GroupID])
            REFERENCES [HR].[tbl_Occupational_Groups] ([GroupID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_jobs_JobType')
    ALTER TABLE [HR].[tbl_jobs]
        ADD CONSTRAINT [FK_jobs_JobType]
            FOREIGN KEY ([JobTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_jobs_LaborRegime')
    ALTER TABLE [HR].[tbl_jobs]
        ADD CONSTRAINT [FK_jobs_LaborRegime]
            FOREIGN KEY ([LaborRegimeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_EmailLogAttachments ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbl_EmailLogAttachments_tbl_EmailLogs')
    ALTER TABLE [HR].[tbl_EmailLogAttachments]
        ADD CONSTRAINT [FK_tbl_EmailLogAttachments_tbl_EmailLogs]
            FOREIGN KEY ([EmailLogID])
            REFERENCES [HR].[tbl_EmailLogs] ([EmailLogID]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbl_EmailLogAttachments_TBL_StoredFile_FileGuid')
    ALTER TABLE [HR].[tbl_EmailLogAttachments]
        ADD CONSTRAINT [FK_tbl_EmailLogAttachments_TBL_StoredFile_FileGuid]
            FOREIGN KEY ([StoredFileGuid])
            REFERENCES [HR].[TBL_StoredFile] ([FileGuid]);
GO

-- --- Tabla: tbl_FinancialCertificationRejectionHistory ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinCertRejectHistory_NewCertificationStatus')
    ALTER TABLE [HR].[tbl_FinancialCertificationRejectionHistory]
        ADD CONSTRAINT [FK_FinCertRejectHistory_NewCertificationStatus]
            FOREIGN KEY ([NewCertificationStatus])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinCertRejectHistory_PreviousRequestStatus')
    ALTER TABLE [HR].[tbl_FinancialCertificationRejectionHistory]
        ADD CONSTRAINT [FK_FinCertRejectHistory_PreviousRequestStatus]
            FOREIGN KEY ([PreviousRequestStatus])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinCertRejectHistory_Request')
    ALTER TABLE [HR].[tbl_FinancialCertificationRejectionHistory]
        ADD CONSTRAINT [FK_FinCertRejectHistory_Request]
            FOREIGN KEY ([RequestID])
            REFERENCES [HR].[tbl_contractRequest] ([RequestID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinCertRejectHistory_RejectionType')
    ALTER TABLE [HR].[tbl_FinancialCertificationRejectionHistory]
        ADD CONSTRAINT [FK_FinCertRejectHistory_RejectionType]
            FOREIGN KEY ([RejectionTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinCertRejectHistory_PreviousCertificationStatus')
    ALTER TABLE [HR].[tbl_FinancialCertificationRejectionHistory]
        ADD CONSTRAINT [FK_FinCertRejectHistory_PreviousCertificationStatus]
            FOREIGN KEY ([PreviousCertificationStatus])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinCertRejectHistory_Certification')
    ALTER TABLE [HR].[tbl_FinancialCertificationRejectionHistory]
        ADD CONSTRAINT [FK_FinCertRejectHistory_Certification]
            FOREIGN KEY ([CertificationID])
            REFERENCES [HR].[tbl_FinancialCertification] ([CertificationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FinCertRejectHistory_NewRequestStatus')
    ALTER TABLE [HR].[tbl_FinancialCertificationRejectionHistory]
        ADD CONSTRAINT [FK_FinCertRejectHistory_NewRequestStatus]
            FOREIGN KEY ([NewRequestStatus])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_Institutions ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Institutions_Province')
    ALTER TABLE [HR].[tbl_Institutions]
        ADD CONSTRAINT [FK_Institutions_Province]
            FOREIGN KEY ([ProvinceID])
            REFERENCES [HR].[tbl_Provinces] ([ProvinceID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Institutions_Canton')
    ALTER TABLE [HR].[tbl_Institutions]
        ADD CONSTRAINT [FK_Institutions_Canton]
            FOREIGN KEY ([CantonID])
            REFERENCES [HR].[tbl_Cantons] ([CantonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Institutions_Country')
    ALTER TABLE [HR].[tbl_Institutions]
        ADD CONSTRAINT [FK_Institutions_Country]
            FOREIGN KEY ([CountryID])
            REFERENCES [HR].[tbl_Countries] ([CountryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Institutions_InstitutionType')
    ALTER TABLE [HR].[tbl_Institutions]
        ADD CONSTRAINT [FK_Institutions_InstitutionType]
            FOREIGN KEY ([InstitutionTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_People ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_Sex')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_Sex]
            FOREIGN KEY ([Sex])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_SpecialNeeds')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_SpecialNeeds]
            FOREIGN KEY ([SpecialNeedsTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_Country')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_Country]
            FOREIGN KEY ([CountryID])
            REFERENCES [HR].[tbl_Countries] ([CountryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_Province')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_Province]
            FOREIGN KEY ([ProvinceID])
            REFERENCES [HR].[tbl_Provinces] ([ProvinceID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_Ethnicity')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_Ethnicity]
            FOREIGN KEY ([EthnicityTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_BloodType')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_BloodType]
            FOREIGN KEY ([BloodTypeTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_Canton')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_Canton]
            FOREIGN KEY ([CantonID])
            REFERENCES [HR].[tbl_Cantons] ([CantonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_MaritalStatus')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_MaritalStatus]
            FOREIGN KEY ([MaritalStatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_IdentType')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_IdentType]
            FOREIGN KEY ([IdentType])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_People_Gender')
    ALTER TABLE [HR].[tbl_People]
        ADD CONSTRAINT [FK_People_Gender]
            FOREIGN KEY ([Gender])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_JobActivities ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_JobActivities_Jobs')
    ALTER TABLE [HR].[tbl_JobActivities]
        ADD CONSTRAINT [FK_JobActivities_Jobs]
            FOREIGN KEY ([JobID])
            REFERENCES [HR].[tbl_jobs] ([JobID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_JobActivities_Activities')
    ALTER TABLE [HR].[tbl_JobActivities]
        ADD CONSTRAINT [FK_JobActivities_Activities]
            FOREIGN KEY ([ActivitiesID])
            REFERENCES [HR].[tbl_Activities] ([ActivitiesID]);
GO

-- --- Tabla: tbl_EducationLevels ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EducationLevels_Person')
    ALTER TABLE [HR].[tbl_EducationLevels]
        ADD CONSTRAINT [FK_EducationLevels_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EducationLevels_EducationLevelType')
    ALTER TABLE [HR].[tbl_EducationLevels]
        ADD CONSTRAINT [FK_EducationLevels_EducationLevelType]
            FOREIGN KEY ([EducationLevelTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EducationLevels_Institution')
    ALTER TABLE [HR].[tbl_EducationLevels]
        ADD CONSTRAINT [FK_EducationLevels_Institution]
            FOREIGN KEY ([InstitutionID])
            REFERENCES [HR].[tbl_Institutions] ([InstitutionID]);
GO

-- --- Tabla: tbl_Addresses ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Addresses_Person')
    ALTER TABLE [HR].[tbl_Addresses]
        ADD CONSTRAINT [FK_Addresses_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Addresses_Canton')
    ALTER TABLE [HR].[tbl_Addresses]
        ADD CONSTRAINT [FK_Addresses_Canton]
            FOREIGN KEY ([CantonID])
            REFERENCES [HR].[tbl_Cantons] ([CantonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Addresses_Country')
    ALTER TABLE [HR].[tbl_Addresses]
        ADD CONSTRAINT [FK_Addresses_Country]
            FOREIGN KEY ([CountryID])
            REFERENCES [HR].[tbl_Countries] ([CountryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Addresses_Province')
    ALTER TABLE [HR].[tbl_Addresses]
        ADD CONSTRAINT [FK_Addresses_Province]
            FOREIGN KEY ([ProvinceID])
            REFERENCES [HR].[tbl_Provinces] ([ProvinceID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Addresses_AddressType')
    ALTER TABLE [HR].[tbl_Addresses]
        ADD CONSTRAINT [FK_Addresses_AddressType]
            FOREIGN KEY ([AddressTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_FamilyBurden ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FamilyBurden_IdentificationType')
    ALTER TABLE [HR].[tbl_FamilyBurden]
        ADD CONSTRAINT [FK_FamilyBurden_IdentificationType]
            FOREIGN KEY ([IdentificationTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FamilyBurden_DisabilityType')
    ALTER TABLE [HR].[tbl_FamilyBurden]
        ADD CONSTRAINT [FK_FamilyBurden_DisabilityType]
            FOREIGN KEY ([DisabilityTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FamilyBurden_Person')
    ALTER TABLE [HR].[tbl_FamilyBurden]
        ADD CONSTRAINT [FK_FamilyBurden_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

-- --- Tabla: tbl_Trainings ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trainings_EventType')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [FK_Trainings_EventType]
            FOREIGN KEY ([EventTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trainings_KnowledgeArea')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [FK_Trainings_KnowledgeArea]
            FOREIGN KEY ([KnowledgeAreaTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trainings_Person')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [FK_Trainings_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trainings_ApprovalType')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [FK_Trainings_ApprovalType]
            FOREIGN KEY ([ApprovalTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trainings_CertificateType')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [FK_Trainings_CertificateType]
            FOREIGN KEY ([CertificateTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- Direccion/modalidad/pais: agregados para academic-promotion (ver 01_tables.sql).
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trainings_TrainingDirectionType')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [FK_Trainings_TrainingDirectionType]
            FOREIGN KEY ([TrainingDirectionTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trainings_ModalityType')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [FK_Trainings_ModalityType]
            FOREIGN KEY ([ModalityTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trainings_Country')
    ALTER TABLE [HR].[tbl_Trainings]
        ADD CONSTRAINT [FK_Trainings_Country]
            FOREIGN KEY ([CountryID])
            REFERENCES [HR].[tbl_Countries] ([CountryID]);
GO

-- --- Tabla: tbl_Languages ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Languages_Person')
    ALTER TABLE [HR].[tbl_Languages]
        ADD CONSTRAINT [FK_Languages_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Languages_LanguageType')
    ALTER TABLE [HR].[tbl_Languages]
        ADD CONSTRAINT [FK_Languages_LanguageType]
            FOREIGN KEY ([LanguageTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Languages_LevelType')
    ALTER TABLE [HR].[tbl_Languages]
        ADD CONSTRAINT [FK_Languages_LevelType]
            FOREIGN KEY ([LevelTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Languages_Country')
    ALTER TABLE [HR].[tbl_Languages]
        ADD CONSTRAINT [FK_Languages_Country]
            FOREIGN KEY ([CountryID])
            REFERENCES [HR].[tbl_Countries] ([CountryID]);
GO

-- --- Tabla: tbl_BankAccounts ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BankAccounts_AccountType')
    ALTER TABLE [HR].[tbl_BankAccounts]
        ADD CONSTRAINT [FK_BankAccounts_AccountType]
            FOREIGN KEY ([AccountTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BankAccounts_Person')
    ALTER TABLE [HR].[tbl_BankAccounts]
        ADD CONSTRAINT [FK_BankAccounts_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

-- --- Tabla: tbl_WorkExperiences ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WorkExperiences_Country')
    ALTER TABLE [HR].[tbl_WorkExperiences]
        ADD CONSTRAINT [FK_WorkExperiences_Country]
            FOREIGN KEY ([CountryID])
            REFERENCES [HR].[tbl_Countries] ([CountryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WorkExperiences_InstitutionType')
    ALTER TABLE [HR].[tbl_WorkExperiences]
        ADD CONSTRAINT [FK_WorkExperiences_InstitutionType]
            FOREIGN KEY ([InstitutionTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WorkExperiences_ExperienceType')
    ALTER TABLE [HR].[tbl_WorkExperiences]
        ADD CONSTRAINT [FK_WorkExperiences_ExperienceType]
            FOREIGN KEY ([ExperienceTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WorkExperiences_Person')
    ALTER TABLE [HR].[tbl_WorkExperiences]
        ADD CONSTRAINT [FK_WorkExperiences_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

-- --- Tabla: tbl_CatastrophicIllnesses ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CatastrophicIllnesses_Person')
    ALTER TABLE [HR].[tbl_CatastrophicIllnesses]
        ADD CONSTRAINT [FK_CatastrophicIllnesses_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CatastrophicIllnesses_IllnessType')
    ALTER TABLE [HR].[tbl_CatastrophicIllnesses]
        ADD CONSTRAINT [FK_CatastrophicIllnesses_IllnessType]
            FOREIGN KEY ([IllnessTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_EmergencyContacts ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmergencyContacts_RelationshipType')
    ALTER TABLE [HR].[tbl_EmergencyContacts]
        ADD CONSTRAINT [FK_EmergencyContacts_RelationshipType]
            FOREIGN KEY ([RelationshipTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmergencyContacts_Person')
    ALTER TABLE [HR].[tbl_EmergencyContacts]
        ADD CONSTRAINT [FK_EmergencyContacts_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

-- --- Tabla: tbl_PersonnelActions ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_UpdatedBy')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActionType_DefaultTemplate')
    ALTER TABLE [HR].[tbl_personnel_action_type]
        ADD CONSTRAINT [FK_PersonnelActionType_DefaultTemplate]
            FOREIGN KEY ([DefaultTemplateId])
            REFERENCES [HR].[tbl_DocumentTemplates] ([TemplateID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_ActionType')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_ActionType]
            FOREIGN KEY ([ActionTypeID])
            REFERENCES [HR].[tbl_personnel_action_type] ([PersonnelActionTypeId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_InstitutionalProcess')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_InstitutionalProcess]
            FOREIGN KEY ([InstitutionalProcess])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_Contract')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_Contract]
            FOREIGN KEY ([ContractID])
            REFERENCES [HR].[tbl_Contracts] ([ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_CreatedBy')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_GeneratedDocument')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_GeneratedDocument]
            FOREIGN KEY ([GeneratedDocumentID])
            REFERENCES [HR].[tbl_GeneratedDocuments] ([DocumentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_StatusTypeId')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_StatusTypeId]
            FOREIGN KEY ([StatusTypeId])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_Employee')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_MagnagementLevel')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_MagnagementLevel]
            FOREIGN KEY ([ManagementLevel])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_Movement')
    ALTER TABLE [HR].[tbl_PersonnelActions]
        ADD CONSTRAINT [FK_PersonnelActions_Movement]
            FOREIGN KEY ([MovementID])
            REFERENCES [HR].[tbl_PersonnelMovements] ([MovementID]);
GO

-- --- Tabla: tbl_PersonnelMovements ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelMovements_Contract')
    ALTER TABLE [HR].[tbl_PersonnelMovements]
        ADD CONSTRAINT [FK_PersonnelMovements_Contract]
            FOREIGN KEY ([ContractID])
            REFERENCES [HR].[tbl_Contracts] ([ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelMovements_Job')
    ALTER TABLE [HR].[tbl_PersonnelMovements]
        ADD CONSTRAINT [FK_PersonnelMovements_Job]
            FOREIGN KEY ([JobID])
            REFERENCES [HR].[tbl_jobs] ([JobID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelMovements_CreatedBy')
    ALTER TABLE [HR].[tbl_PersonnelMovements]
        ADD CONSTRAINT [FK_PersonnelMovements_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelMovements_OriginDepartment')
    ALTER TABLE [HR].[tbl_PersonnelMovements]
        ADD CONSTRAINT [FK_PersonnelMovements_OriginDepartment]
            FOREIGN KEY ([OriginDepartmentID])
            REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelMovements_DestinationDepartment')
    ALTER TABLE [HR].[tbl_PersonnelMovements]
        ADD CONSTRAINT [FK_PersonnelMovements_DestinationDepartment]
            FOREIGN KEY ([DestinationDepartmentID])
            REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelMovements_PersonnelAction')
    ALTER TABLE [HR].[tbl_PersonnelMovements]
        ADD CONSTRAINT [FK_PersonnelMovements_PersonnelAction]
            FOREIGN KEY ([PersonnelActionID])
            REFERENCES [HR].[tbl_PersonnelActions] ([ActionID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelMovements_Employee')
    ALTER TABLE [HR].[tbl_PersonnelMovements]
        ADD CONSTRAINT [FK_PersonnelMovements_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_TimePlanningEmployees ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningEmployees_Plan')
    ALTER TABLE [HR].[tbl_TimePlanningEmployees]
        ADD CONSTRAINT [FK_TimePlanningEmployees_Plan]
            FOREIGN KEY ([PlanID])
            REFERENCES [HR].[tbl_TimePlanning] ([PlanID]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningEmployees_EmployeeStatus')
    ALTER TABLE [HR].[tbl_TimePlanningEmployees]
        ADD CONSTRAINT [FK_TimePlanningEmployees_EmployeeStatus]
            FOREIGN KEY ([EmployeeStatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningEmployees_Employee')
    ALTER TABLE [HR].[tbl_TimePlanningEmployees]
        ADD CONSTRAINT [FK_TimePlanningEmployees_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_AttendanceCalculations ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttCalc_GuardShiftPlanning')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [FK_AttCalc_GuardShiftPlanning]
            FOREIGN KEY ([GuardShiftPlanningID])
            REFERENCES [HR].[tbl_GuardShiftPlanning] ([PlanningID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttCalc_GuardShiftChange')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [FK_AttCalc_GuardShiftChange]
            FOREIGN KEY ([GuardShiftChangeID])
            REFERENCES [HR].[tbl_GuardShiftChanges] ([ShiftChangeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttCalc_EffectiveEmployee')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [FK_AttCalc_EffectiveEmployee]
            FOREIGN KEY ([EffectiveEmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttendanceCalculations_UpdatedBy')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [FK_AttendanceCalculations_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttendanceCalculations_Employee')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [FK_AttendanceCalculations_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttendanceCalculations_CreatedBy')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [FK_AttendanceCalculations_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttCalc_OriginalEmployee')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [FK_AttCalc_OriginalEmployee]
            FOREIGN KEY ([OriginalEmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttendanceCalculations_AppliedSchedule')
    ALTER TABLE [HR].[tbl_AttendanceCalculations]
        ADD CONSTRAINT [FK_AttendanceCalculations_AppliedSchedule]
            FOREIGN KEY ([AppliedScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

-- --- Tabla: tbl_GuardRotationGroups ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardRotationGroups_Parent')
    ALTER TABLE [HR].[tbl_GuardRotationGroups]
        ADD CONSTRAINT [FK_GuardRotationGroups_Parent]
            FOREIGN KEY ([ParentGroupId])
            REFERENCES [HR].[tbl_GuardRotationGroups] ([GroupID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardRotationGroups_LevelType')
    ALTER TABLE [HR].[tbl_GuardRotationGroups]
        ADD CONSTRAINT [FK_GuardRotationGroups_LevelType]
            FOREIGN KEY ([GroupLevelTypeId])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardRotationGroups_CreatedBy')
    ALTER TABLE [HR].[tbl_GuardRotationGroups]
        ADD CONSTRAINT [FK_GuardRotationGroups_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardRotationGroups_UpdatedBy')
    ALTER TABLE [HR].[tbl_GuardRotationGroups]
        ADD CONSTRAINT [FK_GuardRotationGroups_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_GuardShiftCoverageRequirements ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftCovReq_Location')
    ALTER TABLE [HR].[tbl_GuardShiftCoverageRequirements]
        ADD CONSTRAINT [FK_GuardShiftCovReq_Location]
            FOREIGN KEY ([LocationID])
            REFERENCES [HR].[tbl_GuardServiceLocations] ([LocationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftCovReq_CreatedBy')
    ALTER TABLE [HR].[tbl_GuardShiftCoverageRequirements]
        ADD CONSTRAINT [FK_GuardShiftCovReq_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftCovReq_Schedule')
    ALTER TABLE [HR].[tbl_GuardShiftCoverageRequirements]
        ADD CONSTRAINT [FK_GuardShiftCovReq_Schedule]
            FOREIGN KEY ([ScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftCovReq_UpdatedBy')
    ALTER TABLE [HR].[tbl_GuardShiftCoverageRequirements]
        ADD CONSTRAINT [FK_GuardShiftCovReq_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_DepartmentAuthorities ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DeptAuth_Employee')
    ALTER TABLE [HR].[tbl_DepartmentAuthorities]
        ADD CONSTRAINT [FK_DeptAuth_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DeptAuth_AuthType')
    ALTER TABLE [HR].[tbl_DepartmentAuthorities]
        ADD CONSTRAINT [FK_DeptAuth_AuthType]
            FOREIGN KEY ([AuthorityTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DeptAuth_Job')
    ALTER TABLE [HR].[tbl_DepartmentAuthorities]
        ADD CONSTRAINT [FK_DeptAuth_Job]
            FOREIGN KEY ([JobID])
            REFERENCES [HR].[tbl_jobs] ([JobID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DeptAuth_Department')
    ALTER TABLE [HR].[tbl_DepartmentAuthorities]
        ADD CONSTRAINT [FK_DeptAuth_Department]
            FOREIGN KEY ([DepartmentID])
            REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

-- --- Tabla: tbl_GeneratedDocuments ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GeneratedDocuments_Employee')
    ALTER TABLE [HR].[tbl_GeneratedDocuments]
        ADD CONSTRAINT [FK_GeneratedDocuments_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GeneratedDocuments_Template')
    ALTER TABLE [HR].[tbl_GeneratedDocuments]
        ADD CONSTRAINT [FK_GeneratedDocuments_Template]
            FOREIGN KEY ([TemplateID])
            REFERENCES [HR].[tbl_DocumentTemplates] ([TemplateID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GeneratedDocuments_CreatedBy')
    ALTER TABLE [HR].[tbl_GeneratedDocuments]
        ADD CONSTRAINT [FK_GeneratedDocuments_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GeneratedDocuments_StoredFile')
    ALTER TABLE [HR].[tbl_GeneratedDocuments]
        ADD CONSTRAINT [FK_GeneratedDocuments_StoredFile]
            FOREIGN KEY ([StoredFileID])
            REFERENCES [HR].[TBL_StoredFile] ([FileId]);
GO

-- --- Tabla: tbl_AttendancePunches ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttendancePunches_Employee')
    ALTER TABLE [HR].[tbl_AttendancePunches]
        ADD CONSTRAINT [FK_AttendancePunches_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_Payroll ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payroll_Employee')
    ALTER TABLE [HR].[tbl_Payroll]
        ADD CONSTRAINT [FK_Payroll_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_PunchJustifications ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PunchJustifications_Boss')
    ALTER TABLE [HR].[tbl_PunchJustifications]
        ADD CONSTRAINT [FK_PunchJustifications_Boss]
            FOREIGN KEY ([BossEmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PunchJustifications_JustificationType')
    ALTER TABLE [HR].[tbl_PunchJustifications]
        ADD CONSTRAINT [FK_PunchJustifications_JustificationType]
            FOREIGN KEY ([JustificationTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PunchJustifications_PunchType')
    ALTER TABLE [HR].[tbl_PunchJustifications]
        ADD CONSTRAINT [FK_PunchJustifications_PunchType]
            FOREIGN KEY ([PunchTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PunchJustifications_Employee')
    ALTER TABLE [HR].[tbl_PunchJustifications]
        ADD CONSTRAINT [FK_PunchJustifications_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PunchJustifications_CreatedBy')
    ALTER TABLE [HR].[tbl_PunchJustifications]
        ADD CONSTRAINT [FK_PunchJustifications_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_GuardShiftPlanning ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftPlanning_Schedule')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [FK_GuardShiftPlanning_Schedule]
            FOREIGN KEY ([ScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftPlanning_Employee')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [FK_GuardShiftPlanning_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftPlanning_Group')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [FK_GuardShiftPlanning_Group]
            FOREIGN KEY ([GroupID])
            REFERENCES [HR].[tbl_GuardRotationGroups] ([GroupID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftPlanning_Location')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [FK_GuardShiftPlanning_Location]
            FOREIGN KEY ([LocationID])
            REFERENCES [HR].[tbl_GuardServiceLocations] ([LocationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftPlanning_CreatedBy')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [FK_GuardShiftPlanning_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftPlanning_UpdatedBy')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [FK_GuardShiftPlanning_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftPlanning_StatusType')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [FK_GuardShiftPlanning_StatusType]
            FOREIGN KEY ([StatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftPlanning_SourceType')
    ALTER TABLE [HR].[tbl_GuardShiftPlanning]
        ADD CONSTRAINT [FK_GuardShiftPlanning_SourceType]
            FOREIGN KEY ([PlanningSourceTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_GuardGroupRotationPatterns ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardGroupRotPat_Group')
    ALTER TABLE [HR].[tbl_GuardGroupRotationPatterns]
        ADD CONSTRAINT [FK_GuardGroupRotPat_Group]
            FOREIGN KEY ([GroupID])
            REFERENCES [HR].[tbl_GuardRotationGroups] ([GroupID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardGroupRotPat_Pattern')
    ALTER TABLE [HR].[tbl_GuardGroupRotationPatterns]
        ADD CONSTRAINT [FK_GuardGroupRotPat_Pattern]
            FOREIGN KEY ([PatternID])
            REFERENCES [HR].[tbl_RotationPatterns] ([PatternID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardGroupRotPat_UpdatedBy')
    ALTER TABLE [HR].[tbl_GuardGroupRotationPatterns]
        ADD CONSTRAINT [FK_GuardGroupRotPat_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardGroupRotPat_CreatedBy')
    ALTER TABLE [HR].[tbl_GuardGroupRotationPatterns]
        ADD CONSTRAINT [FK_GuardGroupRotPat_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_DocumentTemplates ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocumentTemplates_UpdatedBy')
    ALTER TABLE [HR].[tbl_DocumentTemplates]
        ADD CONSTRAINT [FK_DocumentTemplates_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocumentTemplates_CreatedBy')
    ALTER TABLE [HR].[tbl_DocumentTemplates]
        ADD CONSTRAINT [FK_DocumentTemplates_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_ScheduleChangePlanDetail ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCPD_Status')
    ALTER TABLE [HR].[tbl_ScheduleChangePlanDetail]
        ADD CONSTRAINT [FK_SCPD_Status]
            FOREIGN KEY ([StatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCPD_PrevSchedule')
    ALTER TABLE [HR].[tbl_ScheduleChangePlanDetail]
        ADD CONSTRAINT [FK_SCPD_PrevSchedule]
            FOREIGN KEY ([PreviousScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCPD_PrevEmpSched')
    ALTER TABLE [HR].[tbl_ScheduleChangePlanDetail]
        ADD CONSTRAINT [FK_SCPD_PrevEmpSched]
            FOREIGN KEY ([PreviousEmpScheduleID])
            REFERENCES [HR].[tbl_EmployeeSchedules] ([EmpScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCPD_Employee')
    ALTER TABLE [HR].[tbl_ScheduleChangePlanDetail]
        ADD CONSTRAINT [FK_SCPD_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCPD_Plan')
    ALTER TABLE [HR].[tbl_ScheduleChangePlanDetail]
        ADD CONSTRAINT [FK_SCPD_Plan]
            FOREIGN KEY ([PlanID])
            REFERENCES [HR].[tbl_ScheduleChangePlan] ([PlanID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCPD_ApplEmpSched')
    ALTER TABLE [HR].[tbl_ScheduleChangePlanDetail]
        ADD CONSTRAINT [FK_SCPD_ApplEmpSched]
            FOREIGN KEY ([AppliedEmpScheduleID])
            REFERENCES [HR].[tbl_EmployeeSchedules] ([EmpScheduleID]);
GO

-- --- Tabla: tbl_TimePlanningExecution ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningExecution_VerifiedBy')
    ALTER TABLE [HR].[tbl_TimePlanningExecution]
        ADD CONSTRAINT [FK_TimePlanningExecution_VerifiedBy]
            FOREIGN KEY ([VerifiedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanningExecution_PlanEmployee')
    ALTER TABLE [HR].[tbl_TimePlanningExecution]
        ADD CONSTRAINT [FK_TimePlanningExecution_PlanEmployee]
            FOREIGN KEY ([PlanEmployeeID])
            REFERENCES [HR].[tbl_TimePlanningEmployees] ([PlanEmployeeID]);
GO

-- --- Tabla: tbl_Vacations ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Vacations_Employee')
    ALTER TABLE [HR].[tbl_Vacations]
        ADD CONSTRAINT [FK_Vacations_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_SalaryHistory ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalaryHistory_Contract')
    ALTER TABLE [HR].[tbl_SalaryHistory]
        ADD CONSTRAINT [FK_SalaryHistory_Contract]
            FOREIGN KEY ([ContractID])
            REFERENCES [HR].[tbl_Contracts] ([ContractID]);
GO

-- --- Tabla: tbl_RotationPatterns ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RotationPatterns_UpdatedBy')
    ALTER TABLE [HR].[tbl_RotationPatterns]
        ADD CONSTRAINT [FK_RotationPatterns_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RotationPatterns_Type')
    ALTER TABLE [HR].[tbl_RotationPatterns]
        ADD CONSTRAINT [FK_RotationPatterns_Type]
            FOREIGN KEY ([PatternTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RotationPatterns_CreatedBy')
    ALTER TABLE [HR].[tbl_RotationPatterns]
        ADD CONSTRAINT [FK_RotationPatterns_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_PayrollLines ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PayrollLines_Payroll')
    ALTER TABLE [HR].[tbl_PayrollLines]
        ADD CONSTRAINT [FK_PayrollLines_Payroll]
            FOREIGN KEY ([PayrollID])
            REFERENCES [HR].[tbl_Payroll] ([PayrollID]) ON DELETE CASCADE;
GO

-- --- Tabla: tbl_Publications ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Publications_PublicationType')
    ALTER TABLE [HR].[tbl_Publications]
        ADD CONSTRAINT [FK_Publications_PublicationType]
            FOREIGN KEY ([PublicationTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Publications_Person')
    ALTER TABLE [HR].[tbl_Publications]
        ADD CONSTRAINT [FK_Publications_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Publications_JournalType')
    ALTER TABLE [HR].[tbl_Publications]
        ADD CONSTRAINT [FK_Publications_JournalType]
            FOREIGN KEY ([JournalTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Publications_KnowledgeArea')
    ALTER TABLE [HR].[tbl_Publications]
        ADD CONSTRAINT [FK_Publications_KnowledgeArea]
            FOREIGN KEY ([KnowledgeAreaTypeID])
            REFERENCES [HR].[tbl_KnowledgeArea] ([id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Publications_SubArea')
    ALTER TABLE [HR].[tbl_Publications]
        ADD CONSTRAINT [FK_Publications_SubArea]
            FOREIGN KEY ([SubAreaTypeID])
            REFERENCES [HR].[tbl_KnowledgeArea] ([id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Publications_Area')
    ALTER TABLE [HR].[tbl_Publications]
        ADD CONSTRAINT [FK_Publications_Area]
            FOREIGN KEY ([AreaTypeID])
            REFERENCES [HR].[tbl_KnowledgeArea] ([id]);
GO

-- --- Tabla: tbl_EmployeeAvailabilityBlocks ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmpAvailBlocks_SourceType')
    ALTER TABLE [HR].[tbl_EmployeeAvailabilityBlocks]
        ADD CONSTRAINT [FK_EmpAvailBlocks_SourceType]
            FOREIGN KEY ([SourceTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmpAvailBlocks_Employee')
    ALTER TABLE [HR].[tbl_EmployeeAvailabilityBlocks]
        ADD CONSTRAINT [FK_EmpAvailBlocks_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmpAvailBlocks_CreatedBy')
    ALTER TABLE [HR].[tbl_EmployeeAvailabilityBlocks]
        ADD CONSTRAINT [FK_EmpAvailBlocks_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmpAvailBlocks_StatusType')
    ALTER TABLE [HR].[tbl_EmployeeAvailabilityBlocks]
        ADD CONSTRAINT [FK_EmpAvailBlocks_StatusType]
            FOREIGN KEY ([StatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmpAvailBlocks_UpdatedBy')
    ALTER TABLE [HR].[tbl_EmployeeAvailabilityBlocks]
        ADD CONSTRAINT [FK_EmpAvailBlocks_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_KnowledgeArea ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__tbl_Knowl__paren__7D79703E')
    ALTER TABLE [HR].[tbl_KnowledgeArea]
        ADD CONSTRAINT [FK__tbl_Knowl__paren__7D79703E]
            FOREIGN KEY ([parent_id])
            REFERENCES [HR].[tbl_KnowledgeArea] ([id]);
GO

-- --- Tabla: tbl_GuardServiceLocations ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardServiceLocations_Root')
    ALTER TABLE [HR].[tbl_GuardServiceLocations]
        ADD CONSTRAINT [FK_GuardServiceLocations_Root]
            FOREIGN KEY ([RootLocationID])
            REFERENCES [HR].[tbl_GuardServiceLocations] ([LocationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardServiceLocations_UpdatedBy')
    ALTER TABLE [HR].[tbl_GuardServiceLocations]
        ADD CONSTRAINT [FK_GuardServiceLocations_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardServiceLocations_LocationType')
    ALTER TABLE [HR].[tbl_GuardServiceLocations]
        ADD CONSTRAINT [FK_GuardServiceLocations_LocationType]
            FOREIGN KEY ([LocationTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardServiceLocations_Parent')
    ALTER TABLE [HR].[tbl_GuardServiceLocations]
        ADD CONSTRAINT [FK_GuardServiceLocations_Parent]
            FOREIGN KEY ([ParentLocationID])
            REFERENCES [HR].[tbl_GuardServiceLocations] ([LocationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardServiceLocations_CreatedBy')
    ALTER TABLE [HR].[tbl_GuardServiceLocations]
        ADD CONSTRAINT [FK_GuardServiceLocations_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_GuardRotationGroupEmployees ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardRotGroupEmp_UpdatedBy')
    ALTER TABLE [HR].[tbl_GuardRotationGroupEmployees]
        ADD CONSTRAINT [FK_GuardRotGroupEmp_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardRotGroupEmp_CreatedBy')
    ALTER TABLE [HR].[tbl_GuardRotationGroupEmployees]
        ADD CONSTRAINT [FK_GuardRotGroupEmp_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardRotGroupEmp_Employee')
    ALTER TABLE [HR].[tbl_GuardRotationGroupEmployees]
        ADD CONSTRAINT [FK_GuardRotGroupEmp_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardRotGroupEmp_Group')
    ALTER TABLE [HR].[tbl_GuardRotationGroupEmployees]
        ADD CONSTRAINT [FK_GuardRotGroupEmp_Group]
            FOREIGN KEY ([GroupID])
            REFERENCES [HR].[tbl_GuardRotationGroups] ([GroupID]);
GO

-- --- Tabla: tbl_AdditionalActivities ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AdditionalActivities_Contract')
    ALTER TABLE [HR].[tbl_AdditionalActivities]
        ADD CONSTRAINT [FK_AdditionalActivities_Contract]
            FOREIGN KEY ([ContractID])
            REFERENCES [HR].[tbl_Contracts] ([ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AdditionalActivities_Activities')
    ALTER TABLE [HR].[tbl_AdditionalActivities]
        ADD CONSTRAINT [FK_AdditionalActivities_Activities]
            FOREIGN KEY ([ActivitiesID])
            REFERENCES [HR].[tbl_Activities] ([ActivitiesID]);
GO

-- --- Tabla: tbl_contract_status_history ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contract_status_history_contract')
    ALTER TABLE [HR].[tbl_contract_status_history]
        ADD CONSTRAINT [FK_contract_status_history_contract]
            FOREIGN KEY ([ContractID])
            REFERENCES [HR].[tbl_Contracts] ([ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contract_status_history_status')
    ALTER TABLE [HR].[tbl_contract_status_history]
        ADD CONSTRAINT [FK_contract_status_history_status]
            FOREIGN KEY ([StatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_GuardVacationPlans ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacPlan_Employee')
    ALTER TABLE [HR].[tbl_GuardVacationPlans]
        ADD CONSTRAINT [FK_VacPlan_Employee]
            FOREIGN KEY ([EmployeeId])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacPlan_ApprovedBy')
    ALTER TABLE [HR].[tbl_GuardVacationPlans]
        ADD CONSTRAINT [FK_VacPlan_ApprovedBy]
            FOREIGN KEY ([DirectionApprovedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacPlan_Status')
    ALTER TABLE [HR].[tbl_GuardVacationPlans]
        ADD CONSTRAINT [FK_VacPlan_Status]
            FOREIGN KEY ([StatusTypeId])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacPlan_SubmittedBy')
    ALTER TABLE [HR].[tbl_GuardVacationPlans]
        ADD CONSTRAINT [FK_VacPlan_SubmittedBy]
            FOREIGN KEY ([SubmittedToDirectionBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_contract_type ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contract_type_PersonalContractType')
    ALTER TABLE [HR].[tbl_contract_type]
        ADD CONSTRAINT [FK_contract_type_PersonalContractType]
            FOREIGN KEY ([PersonalContractTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContractType_DelegationTemplate')
    ALTER TABLE [HR].[tbl_contract_type]
        ADD CONSTRAINT [FK_ContractType_DelegationTemplate]
            FOREIGN KEY ([DelegationTemplateId])
            REFERENCES [HR].[tbl_DocumentTemplates] ([TemplateID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contract_type_DocumentTemplateType')
    ALTER TABLE [HR].[tbl_contract_type]
        ADD CONSTRAINT [FK_contract_type_DocumentTemplateType]
            FOREIGN KEY ([DocumentTemplateTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contract_type_DefaultTemplate')
    ALTER TABLE [HR].[tbl_contract_type]
        ADD CONSTRAINT [FK_contract_type_DefaultTemplate]
            FOREIGN KEY ([DefaultTemplateID])
            REFERENCES [HR].[tbl_DocumentTemplates] ([TemplateID]);
GO

-- --- Tabla: tbl_DocumentTemplateFields ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocumentTemplateFields_Template')
    ALTER TABLE [HR].[tbl_DocumentTemplateFields]
        ADD CONSTRAINT [FK_DocumentTemplateFields_Template]
            FOREIGN KEY ([TemplateID])
            REFERENCES [HR].[tbl_DocumentTemplates] ([TemplateID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocumentTemplateFields_UpdatedBy')
    ALTER TABLE [HR].[tbl_DocumentTemplateFields]
        ADD CONSTRAINT [FK_DocumentTemplateFields_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocumentTemplateFields_CreatedBy')
    ALTER TABLE [HR].[tbl_DocumentTemplateFields]
        ADD CONSTRAINT [FK_DocumentTemplateFields_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_GuardAssignmentValidations ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardAssignValids_ValidationType')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [FK_GuardAssignValids_ValidationType]
            FOREIGN KEY ([ValidationTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardAssignValids_ResultType')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [FK_GuardAssignValids_ResultType]
            FOREIGN KEY ([ResultTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardAssignValids_SeverityType')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [FK_GuardAssignValids_SeverityType]
            FOREIGN KEY ([SeverityTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardAssignValids_ShiftChange')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [FK_GuardAssignValids_ShiftChange]
            FOREIGN KEY ([ShiftChangeID])
            REFERENCES [HR].[tbl_GuardShiftChanges] ([ShiftChangeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardAssignValids_Planning')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [FK_GuardAssignValids_Planning]
            FOREIGN KEY ([PlanningID])
            REFERENCES [HR].[tbl_GuardShiftPlanning] ([PlanningID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardAssignValids_UpdatedBy')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [FK_GuardAssignValids_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardAssignValids_CreatedBy')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [FK_GuardAssignValids_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardAssignValids_Employee')
    ALTER TABLE [HR].[tbl_GuardAssignmentValidations]
        ADD CONSTRAINT [FK_GuardAssignValids_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_Subrogations ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Subrogations_SubrogatingEmployee')
    ALTER TABLE [HR].[tbl_Subrogations]
        ADD CONSTRAINT [FK_Subrogations_SubrogatingEmployee]
            FOREIGN KEY ([SubrogatingEmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Subrogations_Permission')
    ALTER TABLE [HR].[tbl_Subrogations]
        ADD CONSTRAINT [FK_Subrogations_Permission]
            FOREIGN KEY ([PermissionID])
            REFERENCES [HR].[tbl_Permissions] ([PermissionID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Subrogations_Vacation')
    ALTER TABLE [HR].[tbl_Subrogations]
        ADD CONSTRAINT [FK_Subrogations_Vacation]
            FOREIGN KEY ([VacationID])
            REFERENCES [HR].[tbl_Vacations] ([VacationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Subrogations_SubrogatedEmployee')
    ALTER TABLE [HR].[tbl_Subrogations]
        ADD CONSTRAINT [FK_Subrogations_SubrogatedEmployee]
            FOREIGN KEY ([SubrogatedEmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_TimeRecoveryPlans ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimeRecoveryPlans_Employee')
    ALTER TABLE [HR].[tbl_TimeRecoveryPlans]
        ADD CONSTRAINT [FK_TimeRecoveryPlans_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimeRecoveryPlans_CreatedBy')
    ALTER TABLE [HR].[tbl_TimeRecoveryPlans]
        ADD CONSTRAINT [FK_TimeRecoveryPlans_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_ScheduleChangePlan ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCP_NewSchedule')
    ALTER TABLE [HR].[tbl_ScheduleChangePlan]
        ADD CONSTRAINT [FK_SCP_NewSchedule]
            FOREIGN KEY ([NewScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCP_Status')
    ALTER TABLE [HR].[tbl_ScheduleChangePlan]
        ADD CONSTRAINT [FK_SCP_Status]
            FOREIGN KEY ([StatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCP_AppliedBy')
    ALTER TABLE [HR].[tbl_ScheduleChangePlan]
        ADD CONSTRAINT [FK_SCP_AppliedBy]
            FOREIGN KEY ([AppliedByID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCP_ApprovedBy')
    ALTER TABLE [HR].[tbl_ScheduleChangePlan]
        ADD CONSTRAINT [FK_SCP_ApprovedBy]
            FOREIGN KEY ([ApprovedByID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SCP_Boss')
    ALTER TABLE [HR].[tbl_ScheduleChangePlan]
        ADD CONSTRAINT [FK_SCP_Boss]
            FOREIGN KEY ([RequestedByBossID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_EmployeeSchedules ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeSchedules_Schedule')
    ALTER TABLE [HR].[tbl_EmployeeSchedules]
        ADD CONSTRAINT [FK_EmployeeSchedules_Schedule]
            FOREIGN KEY ([ScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeSchedules_Employee')
    ALTER TABLE [HR].[tbl_EmployeeSchedules]
        ADD CONSTRAINT [FK_EmployeeSchedules_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_TimePlanning ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_ApprovedBy')
    ALTER TABLE [HR].[tbl_TimePlanning]
        ADD CONSTRAINT [FK_TimePlanning_ApprovedBy]
            FOREIGN KEY ([ApprovedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_UpdatedBy')
    ALTER TABLE [HR].[tbl_TimePlanning]
        ADD CONSTRAINT [FK_TimePlanning_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_SecondApprover')
    ALTER TABLE [HR].[tbl_TimePlanning]
        ADD CONSTRAINT [FK_TimePlanning_SecondApprover]
            FOREIGN KEY ([SecondApprover])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_PlanStatus')
    ALTER TABLE [HR].[tbl_TimePlanning]
        ADD CONSTRAINT [FK_TimePlanning_PlanStatus]
            FOREIGN KEY ([PlanStatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_CreatedBy')
    ALTER TABLE [HR].[tbl_TimePlanning]
        ADD CONSTRAINT [FK_TimePlanning_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimePlanning_OvertimeType')
    ALTER TABLE [HR].[tbl_TimePlanning]
        ADD CONSTRAINT [FK_TimePlanning_OvertimeType]
            FOREIGN KEY ([OvertimeType])
            REFERENCES [HR].[tbl_OvertimeConfig] ([OvertimeType]);
GO

-- --- Tabla: tbl_Employees ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_EmployeeType')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [FK_Employees_EmployeeType]
            FOREIGN KEY ([EmployeeType])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_UpdatedBy')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [FK_Employees_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Department')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [FK_Employees_Department]
            FOREIGN KEY ([DepartmentID])
            REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_CreatedBy')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [FK_Employees_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Boss')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [FK_Employees_Boss]
            FOREIGN KEY ([ImmediateBossID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_JobID')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [FK_Employees_JobID]
            FOREIGN KEY ([JobID])
            REFERENCES [HR].[tbl_jobs] ([JobID]);
GO


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Person')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [FK_Employees_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

-- --- Tabla: tbl_GuardVacationRequests ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_Plan')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_Plan]
            FOREIGN KEY ([GuardVacationPlanId])
            REFERENCES [HR].[tbl_GuardVacationPlans] ([GuardVacationPlanId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_Employee')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_Employee]
            FOREIGN KEY ([EmployeeId])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_ApprovedBy')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_ApprovedBy]
            FOREIGN KEY ([DirectionApprovedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_Vacation')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_Vacation]
            FOREIGN KEY ([VacationId])
            REFERENCES [HR].[tbl_Vacations] ([VacationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_Status')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_Status]
            FOREIGN KEY ([StatusTypeId])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_RequestType')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_RequestType]
            FOREIGN KEY ([RequestTypeId])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_RejectedBy')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_RejectedBy]
            FOREIGN KEY ([RejectedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_RequestedBy')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_RequestedBy]
            FOREIGN KEY ([RequestedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VacReq_SubmittedBy')
    ALTER TABLE [HR].[tbl_GuardVacationRequests]
        ADD CONSTRAINT [FK_VacReq_SubmittedBy]
            FOREIGN KEY ([SubmittedToDirectionBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_Permissions ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Permissions_PermissionType')
    ALTER TABLE [HR].[tbl_Permissions]
        ADD CONSTRAINT [FK_Permissions_PermissionType]
            FOREIGN KEY ([PermissionTypeID])
            REFERENCES [HR].[tbl_PermissionTypes] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Permissions_Employee')
    ALTER TABLE [HR].[tbl_Permissions]
        ADD CONSTRAINT [FK_Permissions_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Permissions_ApprovedBy')
    ALTER TABLE [HR].[tbl_Permissions]
        ADD CONSTRAINT [FK_Permissions_ApprovedBy]
            FOREIGN KEY ([ApprovedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Permissions_Vacation')
    ALTER TABLE [HR].[tbl_Permissions]
        ADD CONSTRAINT [FK_Permissions_Vacation]
            FOREIGN KEY ([VacationID])
            REFERENCES [HR].[tbl_Vacations] ([VacationID]);
GO

-- --- Tabla: tbl_Books ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Books_KnowledgeArea')
    ALTER TABLE [HR].[tbl_Books]
        ADD CONSTRAINT [FK_Books_KnowledgeArea]
            FOREIGN KEY ([KnowledgeAreaTypeID])
            REFERENCES [HR].[tbl_KnowledgeArea] ([id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Books_Area')
    ALTER TABLE [HR].[tbl_Books]
        ADD CONSTRAINT [FK_Books_Area]
            FOREIGN KEY ([AreaTypeID])
            REFERENCES [HR].[tbl_KnowledgeArea] ([id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Books_Country')
    ALTER TABLE [HR].[tbl_Books]
        ADD CONSTRAINT [FK_Books_Country]
            FOREIGN KEY ([CountryID])
            REFERENCES [HR].[tbl_Countries] ([CountryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Books_SubArea')
    ALTER TABLE [HR].[tbl_Books]
        ADD CONSTRAINT [FK_Books_SubArea]
            FOREIGN KEY ([SubAreaTypeID])
            REFERENCES [HR].[tbl_KnowledgeArea] ([id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Books_Type')
    ALTER TABLE [HR].[tbl_Books]
        ADD CONSTRAINT [FK_Books_Type]
            FOREIGN KEY ([bookTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Books_ParticipationType')
    ALTER TABLE [HR].[tbl_Books]
        ADD CONSTRAINT [FK_Books_ParticipationType]
            FOREIGN KEY ([ParticipationTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Books_Person')
    ALTER TABLE [HR].[tbl_Books]
        ADD CONSTRAINT [FK_Books_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

-- --- Tabla: tbl_Overtime ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Overtime_SecondApprover')
    ALTER TABLE [HR].[tbl_Overtime]
        ADD CONSTRAINT [FK_Overtime_SecondApprover]
            FOREIGN KEY ([SecondApprover])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Overtime_OvertimeType')
    ALTER TABLE [HR].[tbl_Overtime]
        ADD CONSTRAINT [FK_Overtime_OvertimeType]
            FOREIGN KEY ([OvertimeType])
            REFERENCES [HR].[tbl_OvertimeConfig] ([OvertimeType]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Overtime_ApprovedBy')
    ALTER TABLE [HR].[tbl_Overtime]
        ADD CONSTRAINT [FK_Overtime_ApprovedBy]
            FOREIGN KEY ([ApprovedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Overtime_Employee')
    ALTER TABLE [HR].[tbl_Overtime]
        ADD CONSTRAINT [FK_Overtime_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- 2026-07-06 (punto 6): trazabilidad al plan de origen. Nullable, no rompe
-- filas existentes/manuales sin plan asociado.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Overtime_PlanEmployee')
    ALTER TABLE [HR].[tbl_Overtime]
        ADD CONSTRAINT [FK_Overtime_PlanEmployee]
            FOREIGN KEY ([PlanEmployeeID])
            REFERENCES [HR].[tbl_TimePlanningEmployees] ([PlanEmployeeID]);
GO

-- --- Tabla: tbl_TimeRecoveryLogs ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimeRecoveryLogs_Plan')
    ALTER TABLE [HR].[tbl_TimeRecoveryLogs]
        ADD CONSTRAINT [FK_TimeRecoveryLogs_Plan]
            FOREIGN KEY ([RecoveryPlanID])
            REFERENCES [HR].[tbl_TimeRecoveryPlans] ([RecoveryPlanID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimeRecoveryLogs_ApprovedBy')
    ALTER TABLE [HR].[tbl_TimeRecoveryLogs]
        ADD CONSTRAINT [FK_TimeRecoveryLogs_ApprovedBy]
            FOREIGN KEY ([ApprovedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_GuardShiftChanges ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_Planning')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_Planning]
            FOREIGN KEY ([PlanningID])
            REFERENCES [HR].[tbl_GuardShiftPlanning] ([PlanningID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_ReplEmp')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_ReplEmp]
            FOREIGN KEY ([ReplacementEmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_RequestedBy')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_RequestedBy]
            FOREIGN KEY ([RequestedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_NewSched')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_NewSched]
            FOREIGN KEY ([NewScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_OrigEmp')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_OrigEmp]
            FOREIGN KEY ([OriginalEmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_OrigSched')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_OrigSched]
            FOREIGN KEY ([OriginalScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_StatusType')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_StatusType]
            FOREIGN KEY ([StatusTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_ChangeType')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_ChangeType]
            FOREIGN KEY ([ChangeTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_ApprovedBy')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_ApprovedBy]
            FOREIGN KEY ([ApprovedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_CreatedBy')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_UpdatedBy')
    ALTER TABLE [HR].[tbl_GuardShiftChanges]
        ADD CONSTRAINT [FK_GuardShiftChanges_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_PersonnelActionStatusHistory ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActionStatusHistory_Action')
    ALTER TABLE [HR].[tbl_PersonnelActionStatusHistory]
        ADD CONSTRAINT [FK_PersonnelActionStatusHistory_Action]
            FOREIGN KEY ([ActionId])
            REFERENCES [HR].[tbl_PersonnelActions] ([ActionID]);
GO

-- --- Tabla: tbl_RotationPatternDetails ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RotationPatternDetails_UpdatedBy')
    ALTER TABLE [HR].[tbl_RotationPatternDetails]
        ADD CONSTRAINT [FK_RotationPatternDetails_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RotationPatternDetails_Schedule')
    ALTER TABLE [HR].[tbl_RotationPatternDetails]
        ADD CONSTRAINT [FK_RotationPatternDetails_Schedule]
            FOREIGN KEY ([ScheduleID])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RotationPatternDetails_Pattern')
    ALTER TABLE [HR].[tbl_RotationPatternDetails]
        ADD CONSTRAINT [FK_RotationPatternDetails_Pattern]
            FOREIGN KEY ([PatternID])
            REFERENCES [HR].[tbl_RotationPatterns] ([PatternID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RotationPatternDetails_CreatedBy')
    ALTER TABLE [HR].[tbl_RotationPatternDetails]
        ADD CONSTRAINT [FK_RotationPatternDetails_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_GeneratedDocumentFields ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GeneratedDocumentFields_Document')
    ALTER TABLE [HR].[tbl_GeneratedDocumentFields]
        ADD CONSTRAINT [FK_GeneratedDocumentFields_Document]
            FOREIGN KEY ([DocumentID])
            REFERENCES [HR].[tbl_GeneratedDocuments] ([DocumentID]);
GO

-- --- Tabla: tbl_contractRequestPerson ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contractRequestPerson_EntrySource')
    ALTER TABLE [HR].[tbl_contractRequestPerson]
        ADD CONSTRAINT [FK_contractRequestPerson_EntrySource]
            FOREIGN KEY ([EntrySourceID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contractRequestPerson_Job')
    ALTER TABLE [HR].[tbl_contractRequestPerson]
        ADD CONSTRAINT [FK_contractRequestPerson_Job]
            FOREIGN KEY ([JobID])
            REFERENCES [HR].[tbl_jobs] ([JobID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contractRequestPerson_Person')
    ALTER TABLE [HR].[tbl_contractRequestPerson]
        ADD CONSTRAINT [FK_contractRequestPerson_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contractRequestPerson_Status')
    ALTER TABLE [HR].[tbl_contractRequestPerson]
        ADD CONSTRAINT [FK_contractRequestPerson_Status]
            FOREIGN KEY ([Status])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contractRequestPerson_Request')
    ALTER TABLE [HR].[tbl_contractRequestPerson]
        ADD CONSTRAINT [FK_contractRequestPerson_Request]
            FOREIGN KEY ([RequestID])
            REFERENCES [HR].[tbl_contractRequest] ([RequestID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_contractRequestPerson_Contract')
    ALTER TABLE [HR].[tbl_contractRequestPerson]
        ADD CONSTRAINT [FK_contractRequestPerson_Contract]
            FOREIGN KEY ([ContractID])
            REFERENCES [HR].[tbl_Contracts] ([ContractID]);
GO

-- --- Tabla: tbl_GuardEmployeeSpecialRules ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SpecialRules_Location')
    ALTER TABLE [HR].[tbl_GuardEmployeeSpecialRules]
        ADD CONSTRAINT [FK_SpecialRules_Location]
            FOREIGN KEY ([FixedLocationId])
            REFERENCES [HR].[tbl_GuardServiceLocations] ([LocationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SpecialRules_Employee')
    ALTER TABLE [HR].[tbl_GuardEmployeeSpecialRules]
        ADD CONSTRAINT [FK_SpecialRules_Employee]
            FOREIGN KEY ([EmployeeId])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SpecialRules_Schedule')
    ALTER TABLE [HR].[tbl_GuardEmployeeSpecialRules]
        ADD CONSTRAINT [FK_SpecialRules_Schedule]
            FOREIGN KEY ([FixedScheduleId])
            REFERENCES [HR].[tbl_Schedules] ([ScheduleID]);
GO

-- --- Tabla: tbl_Contracts ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_GeneratedDocument')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_GeneratedDocument]
            FOREIGN KEY ([GeneratedDocumentID])
            REFERENCES [HR].[tbl_GeneratedDocuments] ([DocumentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_UpdatedBy')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_UpdatedBy]
            FOREIGN KEY ([UpdatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_Department')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_Department]
            FOREIGN KEY ([DepartmentID])
            REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_Parent')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_Parent]
            FOREIGN KEY ([ParentID])
            REFERENCES [HR].[tbl_Contracts] ([ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_Person')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_Person]
            FOREIGN KEY ([PersonID])
            REFERENCES [HR].[tbl_People] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_Job')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_Job]
            FOREIGN KEY ([JobID])
            REFERENCES [HR].[tbl_jobs] ([JobID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_ContractType')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_ContractType]
            FOREIGN KEY ([ContractTypeID])
            REFERENCES [HR].[tbl_contract_type] ([ContractTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_CreatedBy')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_CreatedBy]
            FOREIGN KEY ([CreatedBy])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_Certification')
    ALTER TABLE [HR].[tbl_Contracts]
        ADD CONSTRAINT [FK_Contracts_Certification]
            FOREIGN KEY ([CertificationID])
            REFERENCES [HR].[tbl_FinancialCertification] ([CertificationID]);
GO

-- --- Tabla: tbl_GuardLocationRotationAssignments ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LocationAssign_Priority')
    ALTER TABLE [HR].[tbl_GuardLocationRotationAssignments]
        ADD CONSTRAINT [FK_LocationAssign_Priority]
            FOREIGN KEY ([PriorityTypeId])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LocationAssign_Group')
    ALTER TABLE [HR].[tbl_GuardLocationRotationAssignments]
        ADD CONSTRAINT [FK_LocationAssign_Group]
            FOREIGN KEY ([GroupId])
            REFERENCES [HR].[tbl_GuardRotationGroups] ([GroupID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LocationAssign_Period')
    ALTER TABLE [HR].[tbl_GuardLocationRotationAssignments]
        ADD CONSTRAINT [FK_LocationAssign_Period]
            FOREIGN KEY ([LocationRotationPeriodId])
            REFERENCES [HR].[tbl_GuardLocationRotationPeriods] ([LocationRotationPeriodId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LocationAssign_Employee')
    ALTER TABLE [HR].[tbl_GuardLocationRotationAssignments]
        ADD CONSTRAINT [FK_LocationAssign_Employee]
            FOREIGN KEY ([EmployeeId])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LocationAssign_Location')
    ALTER TABLE [HR].[tbl_GuardLocationRotationAssignments]
        ADD CONSTRAINT [FK_LocationAssign_Location]
            FOREIGN KEY ([LocationId])
            REFERENCES [HR].[tbl_GuardServiceLocations] ([LocationID]);
GO

-- --- Tabla: tbl_TeacherStructure ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherStr_Dedication')
    ALTER TABLE [HR].[tbl_TeacherStructure]
        ADD CONSTRAINT [FK_TeacherStr_Dedication]
            FOREIGN KEY ([DedicationTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherStr_Ladder')
    ALTER TABLE [HR].[tbl_TeacherStructure]
        ADD CONSTRAINT [FK_TeacherStr_Ladder]
            FOREIGN KEY ([LadderID])
            REFERENCES [HR].[tbl_AcademicLadder] ([LadderID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherStr_Department')
    ALTER TABLE [HR].[tbl_TeacherStructure]
        ADD CONSTRAINT [FK_TeacherStr_Department]
            FOREIGN KEY ([DepartmentID])
            REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherStr_Employee')
    ALTER TABLE [HR].[tbl_TeacherStructure]
        ADD CONSTRAINT [FK_TeacherStr_Employee]
            FOREIGN KEY ([EmployeeID])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

-- --- Tabla: tbl_AcademicLadder ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AcadLadder_Next')
    ALTER TABLE [HR].[tbl_AcademicLadder]
        ADD CONSTRAINT [FK_AcadLadder_Next]
            FOREIGN KEY ([NextLadderID])
            REFERENCES [HR].[tbl_AcademicLadder] ([LadderID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AcadLadder_Level')
    ALTER TABLE [HR].[tbl_AcademicLadder]
        ADD CONSTRAINT [FK_AcadLadder_Level]
            FOREIGN KEY ([LevelTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AcadLadder_Category')
    ALTER TABLE [HR].[tbl_AcademicLadder]
        ADD CONSTRAINT [FK_AcadLadder_Category]
            FOREIGN KEY ([CategoryTypeID])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_Departments ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_DepartmentScope')
    ALTER TABLE [HR].[tbl_Departments]
        ADD CONSTRAINT [FK_Departments_DepartmentScope]
            FOREIGN KEY ([DepartmentScope])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_DeanDirector')
    ALTER TABLE [HR].[tbl_Departments]
        ADD CONSTRAINT [FK_Departments_DeanDirector]
            FOREIGN KEY ([DeanDirector])
            REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_Parent')
    ALTER TABLE [HR].[tbl_Departments]
        ADD CONSTRAINT [FK_Departments_Parent]
            FOREIGN KEY ([ParentID])
            REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_DepartmentType')
    ALTER TABLE [HR].[tbl_Departments]
        ADD CONSTRAINT [FK_Departments_DepartmentType]
            FOREIGN KEY ([DepartmentType])
            REFERENCES [HR].[ref_Types] ([TypeID]);
GO

-- --- Tabla: tbl_UserAccessScopes ---
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_HR_UserAccessScopes')
    ALTER TABLE [HR].[tbl_UserAccessScopes]
        ADD CONSTRAINT [PK_HR_UserAccessScopes] PRIMARY KEY CLUSTERED ([Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserAccessScopes_ModuleType')
    ALTER TABLE [HR].[tbl_UserAccessScopes]
        ADD CONSTRAINT [FK_UserAccessScopes_ModuleType]
            FOREIGN KEY ([ModuleTypeId]) REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserAccessScopes_ScopeType')
    ALTER TABLE [HR].[tbl_UserAccessScopes]
        ADD CONSTRAINT [FK_UserAccessScopes_ScopeType]
            FOREIGN KEY ([ScopeTypeId]) REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserAccessScopes_Department')
    ALTER TABLE [HR].[tbl_UserAccessScopes]
        ADD CONSTRAINT [FK_UserAccessScopes_Department]
            FOREIGN KEY ([DepartmentId]) REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserAccessScopes_Employee_Module_Active')
    CREATE NONCLUSTERED INDEX [IX_UserAccessScopes_Employee_Module_Active]
        ON [HR].[tbl_UserAccessScopes] ([EmployeeId], [ModuleTypeId], [IsActive]);
GO

-- --- Tabla: tbl_EmployeeLaborRegime ---
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_HR_EmployeeLaborRegime')
    ALTER TABLE [HR].[tbl_EmployeeLaborRegime]
        ADD CONSTRAINT [PK_HR_EmployeeLaborRegime] PRIMARY KEY CLUSTERED ([Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeLaborRegime_Employee')
    ALTER TABLE [HR].[tbl_EmployeeLaborRegime]
        ADD CONSTRAINT [FK_EmployeeLaborRegime_Employee]
            FOREIGN KEY ([EmployeeId]) REFERENCES [HR].[tbl_Employees] ([EmployeeId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeLaborRegime_RefTypes')
    ALTER TABLE [HR].[tbl_EmployeeLaborRegime]
        ADD CONSTRAINT [FK_EmployeeLaborRegime_RefTypes]
            FOREIGN KEY ([LaborRegimeId]) REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeLaborRegime_Department')
    ALTER TABLE [HR].[tbl_EmployeeLaborRegime]
        ADD CONSTRAINT [FK_EmployeeLaborRegime_Department]
            FOREIGN KEY ([DepartmentId]) REFERENCES [HR].[tbl_Departments] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeLaborRegime_Job')
    ALTER TABLE [HR].[tbl_EmployeeLaborRegime]
        ADD CONSTRAINT [FK_EmployeeLaborRegime_Job]
            FOREIGN KEY ([JobId]) REFERENCES [HR].[tbl_jobs] ([JobID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeLaborRegime_Employee_Active')
    CREATE NONCLUSTERED INDEX [IX_EmployeeLaborRegime_Employee_Active]
        ON [HR].[tbl_EmployeeLaborRegime] ([EmployeeId], [IsActive]);
GO

-- Un solo régimen del mismo tipo activo por empleado a la vez.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeLaborRegime_Employee_Regime_Active')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_EmployeeLaborRegime_Employee_Regime_Active]
        ON [HR].[tbl_EmployeeLaborRegime] ([EmployeeId], [LaborRegimeId])
        WHERE [IsActive] = (1);
GO

-- --- Tabla: tbl_UserAccessScopeHistory ---
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_HR_UserAccessScopeHistory')
    ALTER TABLE [HR].[tbl_UserAccessScopeHistory]
        ADD CONSTRAINT [PK_HR_UserAccessScopeHistory] PRIMARY KEY CLUSTERED ([Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserAccessScopeHistory_Employee')
    CREATE NONCLUSTERED INDEX [IX_UserAccessScopeHistory_Employee]
        ON [HR].[tbl_UserAccessScopeHistory] ([EmployeeId], [ChangeDateTime]);
GO

-- ============================================================
-- BLOQUE 4: CHECK CONSTRAINTS (fechas/rangos)
-- 2026-07-06: agregado tras encontrar tbl_TimePlanning.PlanID=17 con
-- EndDate anterior a StartDate — ese plan nunca podía ejecutarse ni
-- pagarse, sin ningún error visible para quien lo creó.
-- ============================================================

-- WITH NOCHECK: existe una fila real (PlanID=17) que ya viola esta regla
-- (EndDate anterior a StartDate). No se adivina cuál sería la fecha correcta
-- de ese dato histórico — se protege todo INSERT/UPDATE nuevo hacia adelante
-- y se deja esa fila puntual para revisión manual del usuario.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanning_EndDate_GE_StartDate')
    ALTER TABLE [HR].[tbl_TimePlanning] WITH NOCHECK
        ADD CONSTRAINT [CK_TimePlanning_EndDate_GE_StartDate] CHECK ([EndDate] >= [StartDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimeRecoveryPlans_ToTime_GT_FromTime')
    ALTER TABLE [HR].[tbl_TimeRecoveryPlans]
        ADD CONSTRAINT [CK_TimeRecoveryPlans_ToTime_GT_FromTime] CHECK ([ToTime] > [FromTime]);
GO

-- 2026-07-06: sp_Overtime_Price/sp_Payroll_Discounts/sp_Payroll_Subsidies usaban
-- MERGE ... ON 1=0, que nunca hace match, así que cada reproceso del mismo período
-- insertaba líneas de nómina duplicadas. Esta UNIQUE respalda a nivel de esquema
-- el fix de llave real aplicado en esos 3 procedimientos (tbl_PayrollLines estaba
-- vacía en producción al momento de agregarla, no requirió limpieza previa).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_PayrollLines_Payroll_Line_Concept')
    CREATE UNIQUE INDEX [UQ_PayrollLines_Payroll_Line_Concept]
        ON [HR].[tbl_PayrollLines] ([PayrollID], [LineType], [Concept]);
GO

-- 2026-07-06 (propuesta VIGENTE en Acciones de Personal): CHK_PersonnelActions_Status
-- ya existía en producción (creado fuera de estos scripts) sin permitir 'VIGENTE'.
-- Se recrea con el valor agregado — tipos con ReachesVigente=1 ahora transicionan
-- FIRMADO_CARGADO -> VIGENTE automáticamente al cargar el documento firmado.
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_PersonnelActions_Status' AND definition NOT LIKE '%VIGENTE%'
)
    ALTER TABLE [HR].[tbl_PersonnelActions] DROP CONSTRAINT [CHK_PersonnelActions_Status];

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PersonnelActions_Status')
    ALTER TABLE [HR].[tbl_PersonnelActions] WITH CHECK
        ADD CONSTRAINT [CHK_PersonnelActions_Status]
        CHECK ([Status]='ANULADO' OR [Status]='FINALIZADO' OR [Status]='VIGENTE'
               OR [Status]='FIRMADO_CARGADO' OR [Status]='PENDIENTE_FIRMAS'
               OR [Status]='GENERADO' OR [Status]='BORRADOR');
GO
