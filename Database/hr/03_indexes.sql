-- ============================================================
-- ÍNDICES NONCLUSTERED: esquema [HR]
-- ============================================================

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttCalc_GuardShiftPlanning')
    CREATE NONCLUSTERED INDEX [IX_AttCalc_GuardShiftPlanning] ON [HR].[tbl_AttendanceCalculations] ([GuardShiftPlanningID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttendanceCalculations_AppliedSchedule')
    CREATE NONCLUSTERED INDEX [IX_AttendanceCalculations_AppliedSchedule] ON [HR].[tbl_AttendanceCalculations] ([AppliedScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttendanceCalculations_CalculatedAt')
    CREATE NONCLUSTERED INDEX [IX_AttendanceCalculations_CalculatedAt] ON [HR].[tbl_AttendanceCalculations] ([CalculatedAt]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttendanceCalculations_Compliance')
    CREATE NONCLUSTERED INDEX [IX_AttendanceCalculations_Compliance] ON [HR].[tbl_AttendanceCalculations] ([WorkDate], [EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttendanceCalculations_Employee')
    CREATE NONCLUSTERED INDEX [IX_AttendanceCalculations_Employee] ON [HR].[tbl_AttendanceCalculations] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttendanceCalculations_Novelties')
    CREATE NONCLUSTERED INDEX [IX_AttendanceCalculations_Novelties] ON [HR].[tbl_AttendanceCalculations] ([WorkDate], [EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttendanceCalculations_Status')
    CREATE NONCLUSTERED INDEX [IX_AttendanceCalculations_Status] ON [HR].[tbl_AttendanceCalculations] ([Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttendanceCalculations_WorkDate')
    CREATE NONCLUSTERED INDEX [IX_AttendanceCalculations_WorkDate] ON [HR].[tbl_AttendanceCalculations] ([WorkDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_AttendanceCalculations_Employee_WorkDate')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_AttendanceCalculations_Employee_WorkDate] ON [HR].[tbl_AttendanceCalculations] ([EmployeeID], [WorkDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AttendancePunches_Employee_PunchTime')
    CREATE NONCLUSTERED INDEX [IX_AttendancePunches_Employee_PunchTime] ON [HR].[tbl_AttendancePunches] ([EmployeeID], [PunchTime]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_contractRequest_Department_Status')
    CREATE NONCLUSTERED INDEX [IX_contractRequest_Department_Status] ON [HR].[tbl_contractRequest] ([DepartmentID], [Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_contractRequest_Status')
    CREATE NONCLUSTERED INDEX [IX_contractRequest_Status] ON [HR].[tbl_contractRequest] ([Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_contractRequestPerson_Contract')
    CREATE NONCLUSTERED INDEX [IX_contractRequestPerson_Contract] ON [HR].[tbl_contractRequestPerson] ([ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_contractRequestPerson_Pending')
    CREATE NONCLUSTERED INDEX [IX_contractRequestPerson_Pending] ON [HR].[tbl_contractRequestPerson] ([RequestID], [IsActive], [IsHired], [ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_contractRequestPerson_Person')
    CREATE NONCLUSTERED INDEX [IX_contractRequestPerson_Person] ON [HR].[tbl_contractRequestPerson] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_contractRequestPerson_Request')
    CREATE NONCLUSTERED INDEX [IX_contractRequestPerson_Request] ON [HR].[tbl_contractRequestPerson] ([RequestID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_contractRequestPerson_Request_Status_Active')
    CREATE NONCLUSTERED INDEX [IX_contractRequestPerson_Request_Status_Active] ON [HR].[tbl_contractRequestPerson] ([RequestID], [Status], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_contractRequestPerson_Contract_Active')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_contractRequestPerson_Contract_Active] ON [HR].[tbl_contractRequestPerson] ([ContractID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_contractRequestPerson_Request_Person_Active')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_contractRequestPerson_Request_Person_Active] ON [HR].[tbl_contractRequestPerson] ([RequestID], [PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Contracts_Employee')
    CREATE NONCLUSTERED INDEX [IX_Contracts_Employee] ON [HR].[tbl_Contracts] ([PersonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Contracts_Type')
    CREATE NONCLUSTERED INDEX [IX_Contracts_Type] ON [HR].[tbl_Contracts] ([ContractTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_DeptAuth_DateRange')
    CREATE NONCLUSTERED INDEX [IX_DeptAuth_DateRange] ON [HR].[tbl_DepartmentAuthorities] ([StartDate], [EndDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_DeptAuth_Dept_Active')
    CREATE NONCLUSTERED INDEX [IX_DeptAuth_Dept_Active] ON [HR].[tbl_DepartmentAuthorities] ([DepartmentID], [AuthorityTypeID], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_DeptAuth_Employee')
    CREATE NONCLUSTERED INDEX [IX_DeptAuth_Employee] ON [HR].[tbl_DepartmentAuthorities] ([EmployeeID], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Departments_Code')
    CREATE NONCLUSTERED INDEX [IX_Departments_Code] ON [HR].[tbl_Departments] ([Code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Departments_Dean')
    CREATE NONCLUSTERED INDEX [IX_Departments_Dean] ON [HR].[tbl_Departments] ([DeanDirector]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Departments_IsActive')
    CREATE NONCLUSTERED INDEX [IX_Departments_IsActive] ON [HR].[tbl_Departments] ([IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Departments_Parent')
    CREATE NONCLUSTERED INDEX [IX_Departments_Parent] ON [HR].[tbl_Departments] ([ParentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Departments_Type')
    CREATE NONCLUSTERED INDEX [IX_Departments_Type] ON [HR].[tbl_Departments] ([DepartmentType]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_DocumentTemplateFields_Template')
    CREATE NONCLUSTERED INDEX [IX_DocumentTemplateFields_Template] ON [HR].[tbl_DocumentTemplateFields] ([TemplateID], [SortOrder]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_DocumentTemplates_TypeStatus')
    CREATE NONCLUSTERED INDEX [IX_DocumentTemplates_TypeStatus] ON [HR].[tbl_DocumentTemplates] ([TemplateType], [Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_tbl_EmailLayouts_Slug')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_tbl_EmailLayouts_Slug] ON [HR].[tbl_EmailLayouts] ([Slug]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_EmailLogAttachments_EmailLogID')
    CREATE NONCLUSTERED INDEX [IX_tbl_EmailLogAttachments_EmailLogID] ON [HR].[tbl_EmailLogAttachments] ([EmailLogID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_tbl_EmailLogAttachments_EmailLogID_StoredFileGuid')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_tbl_EmailLogAttachments_EmailLogID_StoredFileGuid] ON [HR].[tbl_EmailLogAttachments] ([EmailLogID], [StoredFileGuid]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_EmailLogs_Recipient_SentAt')
    CREATE NONCLUSTERED INDEX [IX_tbl_EmailLogs_Recipient_SentAt] ON [HR].[tbl_EmailLogs] ([Recipient], [SentAt] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_EmailLogs_SentAt')
    CREATE NONCLUSTERED INDEX [IX_tbl_EmailLogs_SentAt] ON [HR].[tbl_EmailLogs] ([SentAt] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_EmpAvailBlocks_EmpDates')
    CREATE NONCLUSTERED INDEX [IX_EmpAvailBlocks_EmpDates] ON [HR].[tbl_EmployeeAvailabilityBlocks] ([EmployeeID], [StartDateTime], [EndDateTime], [StatusTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_EmpAvailBlocks_Source')
    CREATE NONCLUSTERED INDEX [IX_EmpAvailBlocks_Source] ON [HR].[tbl_EmployeeAvailabilityBlocks] ([SourceTable], [SourceID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Employees_Boss')
    CREATE NONCLUSTERED INDEX [IX_Employees_Boss] ON [HR].[tbl_Employees] ([ImmediateBossID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Employees_Department')
    CREATE NONCLUSTERED INDEX [IX_Employees_Department] ON [HR].[tbl_Employees] ([DepartmentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Employees_Email')
    CREATE NONCLUSTERED INDEX [IX_Employees_Email] ON [HR].[tbl_Employees] ([Email]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Employees_HireDate')
    CREATE NONCLUSTERED INDEX [IX_Employees_HireDate] ON [HR].[tbl_Employees] ([HireDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Employees_IsActive')
    CREATE NONCLUSTERED INDEX [IX_Employees_IsActive] ON [HR].[tbl_Employees] ([IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Employees_Type')
    CREATE NONCLUSTERED INDEX [IX_Employees_Type] ON [HR].[tbl_Employees] ([EmployeeType]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_EmpSchedules_EmpID_ValidFrom')
    CREATE NONCLUSTERED INDEX [IX_EmpSchedules_EmpID_ValidFrom] ON [HR].[tbl_EmployeeSchedules] ([EmployeeID], [ValidFrom] DESC, [EmpScheduleID] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FinancialCertification_RejectionType')
    CREATE NONCLUSTERED INDEX [IX_FinancialCertification_RejectionType] ON [HR].[tbl_FinancialCertification] ([RejectionTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FinancialCertification_Request_Status')
    CREATE NONCLUSTERED INDEX [IX_FinancialCertification_Request_Status] ON [HR].[tbl_FinancialCertification] ([RequestID], [Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FinCertRejectHistory_Certification')
    CREATE NONCLUSTERED INDEX [IX_FinCertRejectHistory_Certification] ON [HR].[tbl_FinancialCertificationRejectionHistory] ([CertificationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FinCertRejectHistory_Request')
    CREATE NONCLUSTERED INDEX [IX_FinCertRejectHistory_Request] ON [HR].[tbl_FinancialCertificationRejectionHistory] ([RequestID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GeneratedDocumentFields_Document')
    CREATE NONCLUSTERED INDEX [IX_GeneratedDocumentFields_Document] ON [HR].[tbl_GeneratedDocumentFields] ([DocumentID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GeneratedDocuments_Employee')
    CREATE NONCLUSTERED INDEX [IX_GeneratedDocuments_Employee] ON [HR].[tbl_GeneratedDocuments] ([EmployeeID], [CreatedAt] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GeneratedDocuments_Entity')
    CREATE NONCLUSTERED INDEX [IX_GeneratedDocuments_Entity] ON [HR].[tbl_GeneratedDocuments] ([EntityType], [EntityId], [CreatedAt] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardAssignValids_EmpDate')
    CREATE NONCLUSTERED INDEX [IX_GuardAssignValids_EmpDate] ON [HR].[tbl_GuardAssignmentValidations] ([EmployeeID], [ValidationDate] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SpecialRules_Employee')
    CREATE NONCLUSTERED INDEX [IX_SpecialRules_Employee] ON [HR].[tbl_GuardEmployeeSpecialRules] ([EmployeeId], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SpecialRules_Validity')
    CREATE NONCLUSTERED INDEX [IX_SpecialRules_Validity] ON [HR].[tbl_GuardEmployeeSpecialRules] ([EmployeeId], [ValidFrom], [ValidTo]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardGroupRotPat_Group')
    CREATE NONCLUSTERED INDEX [IX_GuardGroupRotPat_Group] ON [HR].[tbl_GuardGroupRotationPatterns] ([GroupID], [IsActive], [ValidFrom] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LocationAssign_Employee')
    CREATE NONCLUSTERED INDEX [IX_LocationAssign_Employee] ON [HR].[tbl_GuardLocationRotationAssignments] ([EmployeeId], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LocationAssign_Group')
    CREATE NONCLUSTERED INDEX [IX_LocationAssign_Group] ON [HR].[tbl_GuardLocationRotationAssignments] ([GroupId], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LocationAssign_PeriodActive')
    CREATE NONCLUSTERED INDEX [IX_LocationAssign_PeriodActive] ON [HR].[tbl_GuardLocationRotationAssignments] ([LocationRotationPeriodId], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LocationRotPeriods_Dates')
    CREATE NONCLUSTERED INDEX [IX_LocationRotPeriods_Dates] ON [HR].[tbl_GuardLocationRotationPeriods] ([StartDate], [EndDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardRotGroupEmp_Employee')
    CREATE NONCLUSTERED INDEX [IX_GuardRotGroupEmp_Employee] ON [HR].[tbl_GuardRotationGroupEmployees] ([EmployeeID], [ValidFrom] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardRotGroupEmp_Group')
    CREATE NONCLUSTERED INDEX [IX_GuardRotGroupEmp_Group] ON [HR].[tbl_GuardRotationGroupEmployees] ([GroupID], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardRotationGroups_Parent')
    CREATE NONCLUSTERED INDEX [IX_GuardRotationGroups_Parent] ON [HR].[tbl_GuardRotationGroups] ([ParentGroupId]);
GO

-- Filtrado a solo grupos activos: un grupo inactivo libera su GroupCode para reutilizarlo
-- en un grupo nuevo, en vez de bloquearlo indefinidamente.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_GuardRotationGroups_GroupCode')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_GuardRotationGroups_GroupCode] ON [HR].[tbl_GuardRotationGroups] ([GroupCode])
    WHERE [GroupCode] IS NOT NULL AND [IsActive] = (1);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardServiceLocations_Assignable')
    CREATE NONCLUSTERED INDEX [IX_GuardServiceLocations_Assignable] ON [HR].[tbl_GuardServiceLocations] ([IsAssignable], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardServiceLocations_Parent')
    CREATE NONCLUSTERED INDEX [IX_GuardServiceLocations_Parent] ON [HR].[tbl_GuardServiceLocations] ([ParentLocationID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardServiceLocations_Root')
    CREATE NONCLUSTERED INDEX [IX_GuardServiceLocations_Root] ON [HR].[tbl_GuardServiceLocations] ([RootLocationID], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_GuardServiceLocations_Code')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_GuardServiceLocations_Code] ON [HR].[tbl_GuardServiceLocations] ([LocationCode]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardShiftChanges_Planning')
    CREATE NONCLUSTERED INDEX [IX_GuardShiftChanges_Planning] ON [HR].[tbl_GuardShiftChanges] ([PlanningID], [StatusTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardShiftChanges_Replacement')
    CREATE NONCLUSTERED INDEX [IX_GuardShiftChanges_Replacement] ON [HR].[tbl_GuardShiftChanges] ([ReplacementEmployeeID], [StatusTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_GuardShiftChanges_OneActiveAtt')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_GuardShiftChanges_OneActiveAtt] ON [HR].[tbl_GuardShiftChanges] ([PlanningID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardShiftCovReq_LocSchedDay')
    CREATE NONCLUSTERED INDEX [IX_GuardShiftCovReq_LocSchedDay] ON [HR].[tbl_GuardShiftCoverageRequirements] ([LocationID], [ScheduleID], [DayOfWeek], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardShiftPlanning_EmployeeDate')
    CREATE NONCLUSTERED INDEX [IX_GuardShiftPlanning_EmployeeDate] ON [HR].[tbl_GuardShiftPlanning] ([EmployeeID], [WorkDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardShiftPlanning_LocationDateSched')
    CREATE NONCLUSTERED INDEX [IX_GuardShiftPlanning_LocationDateSched] ON [HR].[tbl_GuardShiftPlanning] ([LocationID], [WorkDate], [ScheduleID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GuardShiftPlanning_WorkDate')
    CREATE NONCLUSTERED INDEX [IX_GuardShiftPlanning_WorkDate] ON [HR].[tbl_GuardShiftPlanning] ([WorkDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_GuardShiftPlanning_NoDoubleActiveShift')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_GuardShiftPlanning_NoDoubleActiveShift] ON [HR].[tbl_GuardShiftPlanning] ([EmployeeID], [WorkDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_VacPlan_EmployeeYear')
    CREATE NONCLUSTERED INDEX [IX_VacPlan_EmployeeYear] ON [HR].[tbl_GuardVacationPlans] ([EmployeeId], [VacationYear]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_VacPlan_Status')
    CREATE NONCLUSTERED INDEX [IX_VacPlan_Status] ON [HR].[tbl_GuardVacationPlans] ([StatusTypeId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_VacReq_EmployeeStatus')
    CREATE NONCLUSTERED INDEX [IX_VacReq_EmployeeStatus] ON [HR].[tbl_GuardVacationRequests] ([EmployeeId], [StatusTypeId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_VacReq_Plan')
    CREATE NONCLUSTERED INDEX [IX_VacReq_Plan] ON [HR].[tbl_GuardVacationRequests] ([GuardVacationPlanId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Holidays_Date')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Holidays_Date] ON [HR].[tbl_Holidays] ([HolidayDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Jobs_GroupID')
    CREATE NONCLUSTERED INDEX [IX_tbl_Jobs_GroupID] ON [HR].[tbl_jobs] ([GroupID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Jobs_GroupID_IsActive')
    CREATE NONCLUSTERED INDEX [IX_tbl_Jobs_GroupID_IsActive] ON [HR].[tbl_jobs] ([GroupID], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Jobs_IsActive')
    CREATE NONCLUSTERED INDEX [IX_tbl_Jobs_IsActive] ON [HR].[tbl_jobs] ([IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Jobs_JobTypeID')
    CREATE NONCLUSTERED INDEX [IX_tbl_Jobs_JobTypeID] ON [HR].[tbl_jobs] ([JobTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Groups_DegreeID')
    CREATE NONCLUSTERED INDEX [IX_tbl_Groups_DegreeID] ON [HR].[tbl_Occupational_Groups] ([DegreeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Groups_DegreeID_IsActive')
    CREATE NONCLUSTERED INDEX [IX_tbl_Groups_DegreeID_IsActive] ON [HR].[tbl_Occupational_Groups] ([DegreeID], [IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Groups_Description')
    CREATE NONCLUSTERED INDEX [IX_tbl_Groups_Description] ON [HR].[tbl_Occupational_Groups] ([Description]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Groups_IsActive')
    CREATE NONCLUSTERED INDEX [IX_tbl_Groups_IsActive] ON [HR].[tbl_Occupational_Groups] ([IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_People_Canton')
    CREATE NONCLUSTERED INDEX [IX_People_Canton] ON [HR].[tbl_People] ([CantonID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_People_Country')
    CREATE NONCLUSTERED INDEX [IX_People_Country] ON [HR].[tbl_People] ([CountryID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_People_Email')
    CREATE NONCLUSTERED INDEX [IX_People_Email] ON [HR].[tbl_People] ([Email]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_People_IDCard')
    CREATE NONCLUSTERED INDEX [IX_People_IDCard] ON [HR].[tbl_People] ([IDCard]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_People_IsActive')
    CREATE NONCLUSTERED INDEX [IX_People_IsActive] ON [HR].[tbl_People] ([IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_People_LastName')
    CREATE NONCLUSTERED INDEX [IX_People_LastName] ON [HR].[tbl_People] ([LastName]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_People_Province')
    CREATE NONCLUSTERED INDEX [IX_People_Province] ON [HR].[tbl_People] ([ProvinceID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Permissions_Employee_Status_Dates')
    CREATE NONCLUSTERED INDEX [IX_Permissions_Employee_Status_Dates] ON [HR].[tbl_Permissions] ([EmployeeID], [Status], [StartDate], [EndDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_personnel_action_type_IsActive')
    CREATE NONCLUSTERED INDEX [IX_tbl_personnel_action_type_IsActive] ON [HR].[tbl_personnel_action_type] ([IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_personnel_action_type_Numbering')
    CREATE NONCLUSTERED INDEX [IX_tbl_personnel_action_type_Numbering] ON [HR].[tbl_personnel_action_type] ([NumberingYear], [NumberingPrefix]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_personnel_action_type_TemplateCode')
    CREATE NONCLUSTERED INDEX [IX_tbl_personnel_action_type_TemplateCode] ON [HR].[tbl_personnel_action_type] ([TemplateCode]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PersonnelActions_Contract')
    CREATE NONCLUSTERED INDEX [IX_PersonnelActions_Contract] ON [HR].[tbl_PersonnelActions] ([ContractID], [ActionDate] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PersonnelActions_EmployeeStatus')
    CREATE NONCLUSTERED INDEX [IX_PersonnelActions_EmployeeStatus] ON [HR].[tbl_PersonnelActions] ([EmployeeID], [Status], [ActionDate] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_PersonnelActions_ActionNumber')
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_PersonnelActions_ActionNumber] ON [HR].[tbl_PersonnelActions] ([ActionNumber]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PunchJustifications_Employee_Status')
    CREATE NONCLUSTERED INDEX [IX_PunchJustifications_Employee_Status] ON [HR].[tbl_PunchJustifications] ([EmployeeID], [Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ReportAudit_GeneratedAt')
    CREATE NONCLUSTERED INDEX [IX_ReportAudit_GeneratedAt] ON [HR].[tbl_ReportAudit] ([GeneratedAt] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ReportAudit_ReportType')
    CREATE NONCLUSTERED INDEX [IX_ReportAudit_ReportType] ON [HR].[tbl_ReportAudit] ([ReportType]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ReportAudit_UserId')
    CREATE NONCLUSTERED INDEX [IX_ReportAudit_UserId] ON [HR].[tbl_ReportAudit] ([UserId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_RotationPatterns_PatternCode')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_RotationPatterns_PatternCode] ON [HR].[tbl_RotationPatterns] ([PatternCode]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SCP_ApplyDate')
    CREATE NONCLUSTERED INDEX [IX_SCP_ApplyDate] ON [HR].[tbl_ScheduleChangePlan] ([EffectiveApplyDate], [StatusTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SCP_Boss')
    CREATE NONCLUSTERED INDEX [IX_SCP_Boss] ON [HR].[tbl_ScheduleChangePlan] ([RequestedByBossID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SCP_EffDate')
    CREATE NONCLUSTERED INDEX [IX_SCP_EffDate] ON [HR].[tbl_ScheduleChangePlan] ([EffectiveDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SCP_Status')
    CREATE NONCLUSTERED INDEX [IX_SCP_Status] ON [HR].[tbl_ScheduleChangePlan] ([StatusTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SCPD_Employee')
    CREATE NONCLUSTERED INDEX [IX_SCPD_Employee] ON [HR].[tbl_ScheduleChangePlanDetail] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SCPD_Plan')
    CREATE NONCLUSTERED INDEX [IX_SCPD_Plan] ON [HR].[tbl_ScheduleChangePlanDetail] ([PlanID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SCPD_Status')
    CREATE NONCLUSTERED INDEX [IX_SCPD_Status] ON [HR].[tbl_ScheduleChangePlanDetail] ([StatusTypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_tbl_Schedules_ScheduleCode')
    CREATE NONCLUSTERED INDEX [IX_tbl_Schedules_ScheduleCode] ON [HR].[tbl_Schedules] ([ScheduleCode]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TBL_StoredFile_ByDirectoryYear')
    CREATE NONCLUSTERED INDEX [IX_TBL_StoredFile_ByDirectoryYear] ON [HR].[TBL_StoredFile] ([DirectoryCode], [UploadYear]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TBL_StoredFile_ByEntity')
    CREATE NONCLUSTERED INDEX [IX_TBL_StoredFile_ByEntity] ON [HR].[TBL_StoredFile] ([DirectoryCode], [EntityType], [EntityId], [UploadYear]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TBL_StoredFile_Sha256')
    CREATE NONCLUSTERED INDEX [IX_TBL_StoredFile_Sha256] ON [HR].[TBL_StoredFile] ([Sha256]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_TBL_StoredFile_FileGuid')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_TBL_StoredFile_FileGuid] ON [HR].[TBL_StoredFile] ([FileGuid]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_TBL_StoredFile_NoDuplicate_Active')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_TBL_StoredFile_NoDuplicate_Active] ON [HR].[TBL_StoredFile] ([FilePathHash]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TimeBalanceMovements_Employee_MovementAt')
    CREATE NONCLUSTERED INDEX [IX_TimeBalanceMovements_Employee_MovementAt] ON [HR].[tbl_TimeBalanceMovements] ([EmployeeID], [MovementAt] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TimeBalanceMovements_Employee_SourceID')
    CREATE NONCLUSTERED INDEX [IX_TimeBalanceMovements_Employee_SourceID] ON [HR].[tbl_TimeBalanceMovements] ([EmployeeID], [SourceID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TimeBalances_LastUpdated')
    CREATE NONCLUSTERED INDEX [IX_TimeBalances_LastUpdated] ON [HR].[tbl_TimeBalances] ([LastUpdated]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_tbl_TimePlanningEmployees_PlanID_EmployeeID')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_tbl_TimePlanningEmployees_PlanID_EmployeeID] ON [HR].[tbl_TimePlanningEmployees] ([PlanID], [EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TimeRecoveryLogs_PlanID_Date')
    CREATE NONCLUSTERED INDEX [IX_TimeRecoveryLogs_PlanID_Date] ON [HR].[tbl_TimeRecoveryLogs] ([RecoveryPlanID], [ExecutedDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TimeRecoveryPlans_Employee')
    CREATE NONCLUSTERED INDEX [IX_TimeRecoveryPlans_Employee] ON [HR].[tbl_TimeRecoveryPlans] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Vacations_Employee_Status_Dates')
    CREATE NONCLUSTERED INDEX [IX_Vacations_Employee_Status_Dates] ON [HR].[tbl_Vacations] ([EmployeeID], [Status], [StartDate], [EndDate]);
GO

-- ============================================================
-- REPORTES V2 — columna y índices para reportes de gestión RH
-- Agregado: 2026-06-09
-- ============================================================

-- Nueva columna ActionCategory en tbl_Personnel_Action_Type
-- Clasifica el tipo de acción: MOVEMENT | ENTRY | ECONOMIC | LEAVE | DISCIPLINARY | EXIT
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'HR'
      AND TABLE_NAME   = 'tbl_Personnel_Action_Type'
      AND COLUMN_NAME  = 'ActionCategory'
)
    ALTER TABLE [HR].[tbl_Personnel_Action_Type]
        ADD [ActionCategory] NVARCHAR(30) NULL;
GO

-- tbl_Contracts: filtro por periodo + dependencia (reporte contratos)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Contracts_StartDate_DepartmentID')
    CREATE NONCLUSTERED INDEX [IX_Contracts_StartDate_DepartmentID]
        ON [HR].[tbl_Contracts] ([StartDate], [DepartmentID])
        INCLUDE ([ContractID], [PersonID], [ContractTypeID], [Status], [EndDate]);
GO

-- tbl_Contracts: filtro por estado (contratos vigentes / por estado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Contracts_Status')
    CREATE NONCLUSTERED INDEX [IX_Contracts_Status]
        ON [HR].[tbl_Contracts] ([Status])
        INCLUDE ([ContractID], [PersonID], [DepartmentID], [StartDate], [EndDate]);
GO

-- tbl_PersonnelActions: cubre nuevos ingresos donde EmployeeID aún es NULL
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PersonnelActions_PersonId_ActionDate')
    CREATE NONCLUSTERED INDEX [IX_PersonnelActions_PersonId_ActionDate]
        ON [HR].[tbl_PersonnelActions] ([PersonId], [ActionDate] DESC)
        INCLUDE ([ActionID], [ActionTypeID], [Status], [EffectiveDate], [EndDate]);
GO

-- tbl_contractRequest: filtro por fecha de inicio (IX_contractRequest_Status ya existe)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_contractRequest_StartDate')
    CREATE NONCLUSTERED INDEX [IX_contractRequest_StartDate]
        ON [HR].[tbl_contractRequest] ([StartDate])
        INCLUDE ([RequestID], [DepartmentId], [Status]);
GO

-- tbl_FinancialCertification: filtro por estado independiente (ya existe uno por RequestID+Status)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FinancialCertification_Status')
    CREATE NONCLUSTERED INDEX [IX_FinancialCertification_Status]
        ON [HR].[tbl_FinancialCertification] ([Status])
        INCLUDE ([CertificationID], [RequestID], [CertCode], [CertBudgetDate]);
GO

-- tbl_FinancialCertification: filtro por fecha de certificación presupuestaria
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FinancialCertification_CertBudgetDate')
    CREATE NONCLUSTERED INDEX [IX_FinancialCertification_CertBudgetDate]
        ON [HR].[tbl_FinancialCertification] ([CertBudgetDate])
        INCLUDE ([CertificationID], [RequestID], [Status]);
GO

-- tbl_Permissions: filtro por rango de fechas + estado (IX_Permissions_Employee_Status_Dates
-- ya cubre consultas por empleado; este cubre consultas globales por periodo)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Permissions_StartDate_Status')
    CREATE NONCLUSTERED INDEX [IX_Permissions_StartDate_Status]
        ON [HR].[tbl_Permissions] ([StartDate], [Status])
        INCLUDE ([PermissionId], [EmployeeID], [PermissionTypeID], [EndDate]);
GO
