-- ============================================================
-- TABLAS: esquema [HR]
-- Generado: 2026-05-29
-- ============================================================

SET NOCOUNT ON;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[ref_Types]') IS NULL
CREATE TABLE [HR].[ref_Types] (
    [TypeID] INT IDENTITY(1,1) NOT NULL,
    [Category] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(255) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [SortOrder] INT DEFAULT ((0)) NULL,
    [Metadata] NVARCHAR(MAX) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[stg_attendance]') IS NULL
CREATE TABLE [HR].[stg_attendance] (
    [Cedula] NVARCHAR(50) NULL,
    [PunchTime] DATETIME2 NULL,
    [PunchType] VARCHAR(50) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[stg_EmployeeScheduleLoad]') IS NULL
CREATE TABLE [HR].[stg_EmployeeScheduleLoad] (
    [StagingID] INT IDENTITY(1,1) NOT NULL,
    [SourceFile] NVARCHAR(150) NOT NULL,
    [SourceSheet] NVARCHAR(100) NULL,
    [RowNum] INT NULL,
    [Cedula] NVARCHAR(20) NOT NULL,
    [NombreCompleto] NVARCHAR(200) NULL,
    [DependenciaTexto] NVARCHAR(200) NULL,
    [SchedulerID] INT NULL,
    [SchedulerName] NVARCHAR(200) NULL,
    [Estado] NVARCHAR(50) NULL,
    [EmployeeType] INT NULL,
    [Observacion] NVARCHAR(500) NULL,
    [IsProcessed] BIT DEFAULT ((0)) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[stg_People]') IS NULL
CREATE TABLE [HR].[stg_People] (
    [IDCard] NVARCHAR(20) NULL,
    [FirstName] NVARCHAR(100) NULL,
    [LastName] NVARCHAR(100) NULL,
    [IdentTypeName] NVARCHAR(50) NULL,
    [Email] NVARCHAR(150) NULL,
    [EmailWork] NVARCHAR(150) NULL,
    [Phone] NVARCHAR(30) NULL,
    [BirthDate] DATE NULL,
    [SexName] NVARCHAR(50) NULL,
    [GenderName] NVARCHAR(50) NULL,
    [MaritalStatusName] NVARCHAR(50) NULL,
    [EthnicityName] NVARCHAR(50) NULL,
    [BloodTypeName] NVARCHAR(10) NULL,
    [SpecialNeedsName] NVARCHAR(100) NULL,
    [DisabilityDescription] NVARCHAR(200) NULL,
    [DisabilityPercentage] DECIMAL(5,2) NULL,
    [CONADISCard] NVARCHAR(50) NULL,
    [MilitaryCard] NVARCHAR(50) NULL,
    [MotherName] NVARCHAR(100) NULL,
    [FatherName] NVARCHAR(100) NULL,
    [CountryID] NVARCHAR(10) NULL,
    [ProvinceID] NVARCHAR(10) NULL,
    [CantonID] NVARCHAR(10) NULL,
    [YearsOfResidence] INT NULL,
    [Address] NVARCHAR(255) NULL,
    [IsActive] BIT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_AcademicLadder]') IS NULL
CREATE TABLE [HR].[tbl_AcademicLadder] (
    [LadderID] INT IDENTITY(1,1) NOT NULL,
    [Code] VARCHAR(30) NOT NULL,
    [Name] NVARCHAR(120) NOT NULL,
    [CategoryTypeID] INT NULL,
    [LevelTypeID] INT NULL,
    [Sequence] INT NOT NULL,
    [NextLadderID] INT NULL,
    [MinYearsService] INT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [DedicationTypeID] INT NULL,
    [BaseRMU] DECIMAL(10,2) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Activities]') IS NULL
CREATE TABLE [HR].[tbl_Activities] (
    [ActivitiesID] INT IDENTITY(1,1) NOT NULL,
    [Description] TEXT NULL,
    [ActivitiesType] NVARCHAR(20) DEFAULT ('LABORAL') NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_AdditionalActivities]') IS NULL
CREATE TABLE [HR].[tbl_AdditionalActivities] (
    [ActivitiesID] INT NOT NULL,
    [ContractID] INT NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Addresses]') IS NULL
CREATE TABLE [HR].[tbl_Addresses] (
    [AddressID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [AddressTypeID] INT NOT NULL,
    [CountryID] NVARCHAR(10) NOT NULL,
    [ProvinceID] NVARCHAR(10) NOT NULL,
    [CantonID] NVARCHAR(10) NOT NULL,
    [Parish] NVARCHAR(100) NULL,
    [Neighborhood] NVARCHAR(100) NULL,
    [MainStreet] NVARCHAR(100) NOT NULL,
    [SecondaryStreet] NVARCHAR(100) NULL,
    [HouseNumber] NVARCHAR(20) NULL,
    [Reference] NVARCHAR(255) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_AttendanceCalcLog]') IS NULL
CREATE TABLE [HR].[tbl_AttendanceCalcLog] (
    [LogID] BIGINT IDENTITY(1,1) NOT NULL,
    [LoggedAt] DATETIME2 DEFAULT (sysutcdatetime()) NOT NULL,
    [FromDate] DATE NOT NULL,
    [ToDate] DATE NOT NULL,
    [EmployeeID] INT NULL,
    [WorkDate] DATE NULL,
    [RequiredMin] INT NULL,
    [EntryTime] TIME NULL,
    [ExitTime] TIME NULL,
    [HasLunch] BIT NULL,
    [LunchStart] TIME NULL,
    [LunchEnd] TIME NULL,
    [LunchMinRow] INT NULL,
    [EntryDT] DATETIME2 NULL,
    [ExitDT] DATETIME2 NULL,
    [LunchStartDT] DATETIME2 NULL,
    [LunchEndDT] DATETIME2 NULL,
    [FirstIn] DATETIME2 NULL,
    [LastOut] DATETIME2 NULL,
    [WorkedGrossMin] INT NULL,
    [RawWorkedMin] INT NULL,
    [MorningStartDT] DATETIME2 NULL,
    [MorningEndDT] DATETIME2 NULL,
    [OverlapMorningMin] INT NULL,
    [AfternoonStartDT] DATETIME2 NULL,
    [AfternoonEndDT] DATETIME2 NULL,
    [OverlapAfternoonMin] INT NULL,
    [ScheduledWorkedMin] INT NULL,
    [OffScheduleMin] INT NULL,
    [TardinessMin] INT NULL,
    [MinutesLate] INT NULL,
    [NightMinutes] INT NULL,
    [RegularFinalMin] INT NULL,
    [SameRegVsTotalFlag] BIT NULL,
    [SameRegVsTotalReason] NVARCHAR(200) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_AttendanceCalculations]') IS NULL
CREATE TABLE [HR].[tbl_AttendanceCalculations] (
    [CalculationID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [WorkDate] DATE NOT NULL,
    [FirstPunchIn] DATETIME2 NULL,
    [LastPunchOut] DATETIME2 NULL,
    [TotalWorkedMinutes] INT DEFAULT ((0)) NOT NULL,
    [RegularMinutes] INT DEFAULT ((0)) NOT NULL,
    [OvertimeMinutes] INT DEFAULT ((0)) NOT NULL,
    [NightMinutes] INT DEFAULT ((0)) NOT NULL,
    [HolidayMinutes] INT DEFAULT ((0)) NOT NULL,
    [RequiredMinutes] INT DEFAULT ((0)) NOT NULL,
    [ScheduledWorkedMin] INT DEFAULT ((0)) NOT NULL,
    [OffScheduleMin] INT DEFAULT ((0)) NOT NULL,
    [AbsentMinutes] INT DEFAULT ((0)) NOT NULL,
    [MinutesLate] INT DEFAULT ((0)) NOT NULL,
    [TardinessMin] INT DEFAULT ((0)) NOT NULL,
    [EarlyLeaveMinutes] INT DEFAULT ((0)) NOT NULL,
    [PermissionMinutes] INT DEFAULT ((0)) NOT NULL,
    [VacationMinutes] INT DEFAULT ((0)) NOT NULL,
    [JustificationMinutes] INT DEFAULT ((0)) NOT NULL,
    [MedicalLeaveMinutes] INT DEFAULT ((0)) NOT NULL,
    [PaidLeaveMinutes] INT DEFAULT ((0)) NOT NULL,
    [UnpaidLeaveMinutes] INT DEFAULT ((0)) NOT NULL,
    [VacationDeductedMinutes] INT DEFAULT ((0)) NOT NULL,
    [RecoveredMinutes] INT DEFAULT ((0)) NOT NULL,
    [JustificationApply] BIT DEFAULT ((0)) NOT NULL,
    [HasPermission] BIT DEFAULT ((0)) NOT NULL,
    [HasVacation] BIT DEFAULT ((0)) NOT NULL,
    [HasJustification] BIT DEFAULT ((0)) NOT NULL,
    [HasMedicalLeave] BIT DEFAULT ((0)) NOT NULL,
    [HasManualAdjustment] BIT DEFAULT ((0)) NOT NULL,
    [FoodSubsidy] INT DEFAULT ((0)) NOT NULL,
    [AppliedScheduleID] INT NULL,
    [ScheduledEntryTime] TIME NULL,
    [ScheduledExitTime] TIME NULL,
    [ScheduledLunchStart] TIME NULL,
    [ScheduledLunchEnd] TIME NULL,
    [ScheduledHasLunchBreak] BIT DEFAULT ((0)) NOT NULL,
    [ScheduledMinutes] INT DEFAULT ((0)) NOT NULL,
    [Status] NVARCHAR(20) DEFAULT ('Pending') NOT NULL,
    [CalculatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CalculationVersion] INT DEFAULT ((1)) NOT NULL,
    [CalculationSource] NVARCHAR(30) DEFAULT ('System') NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [GuardShiftPlanningID] INT NULL,
    [GuardShiftChangeID] INT NULL,
    [OriginalEmployeeID] INT NULL,
    [EffectiveEmployeeID] INT NULL,
    [IsReplacement] BIT DEFAULT ((0)) NOT NULL
);
GO

-- 2026-07-06: separar los dos sistemas que antes colisionaban en la misma
-- columna. OvertimeMinutes/RecoveredMinutes NO cambian de significado — se
-- agregan estos 2 campos para que cada sistema tenga su propio lugar:
--   DetectedOvertimeMinutes: detección automática de sp_ProcessAttendanceBaseDay
--     (trabajado fuera de horario), antes de que la planificación verificada
--     sobreescriba OvertimeMinutes con el monto realmente autorizado/pagado.
--   RecoveryExecutedMinutes: minutos ejecutados por sp_ProcessTimePlanningForEmployeeDay
--     contra un plan tbl_TimePlanning (PlanType='Recovery') — abona a
--     tbl_TimeBalances.RecoveryPendingMin, NO perdona la ausencia del día
--     (eso lo hace RecoveredMinutes, vía sp_ProcessAttendanceRecoveryDay).
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_AttendanceCalculations]') AND name = 'DetectedOvertimeMinutes'
)
    ALTER TABLE [HR].[tbl_AttendanceCalculations] ADD [DetectedOvertimeMinutes] INT DEFAULT ((0)) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_AttendanceCalculations]') AND name = 'RecoveryExecutedMinutes'
)
    ALTER TABLE [HR].[tbl_AttendanceCalculations] ADD [RecoveryExecutedMinutes] INT DEFAULT ((0)) NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_AttendancePunches]') IS NULL
CREATE TABLE [HR].[tbl_AttendancePunches] (
    [PunchID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [PunchTime] DATETIME2 NOT NULL,
    [PunchType] NVARCHAR(10) NOT NULL,
    [DeviceID] NVARCHAR(60) NULL,
    [Longitude] DECIMAL(10,7) NULL,
    [Latitude] DECIMAL(10,7) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [IpAddress] NVARCHAR(60) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Audit]') IS NULL
CREATE TABLE [HR].[tbl_Audit] (
    [AuditID] BIGINT IDENTITY(1,1) NOT NULL,
    [TableName] SYSNAME NOT NULL,
    [Action] NVARCHAR(20) NOT NULL,
    [RecordID] NVARCHAR(64) NOT NULL,
    [UserName] SYSNAME DEFAULT (suser_sname()) NOT NULL,
    [DATETIME2] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [Details] NVARCHAR(MAX) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_BankAccounts]') IS NULL
CREATE TABLE [HR].[tbl_BankAccounts] (
    [AccountID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [FinancialInstitution] NVARCHAR(150) NOT NULL,
    [AccountTypeID] INT NOT NULL,
    [AccountNumber] NVARCHAR(50) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Books]') IS NULL
CREATE TABLE [HR].[tbl_Books] (
    [BookID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [Title] NVARCHAR(300) NOT NULL,
    [PeerReviewed] BIT NULL,
    [ISBN] NVARCHAR(20) NULL,
    [Publisher] NVARCHAR(200) NULL,
    [CountryID] NVARCHAR(10) NULL,
    [City] NVARCHAR(100) NULL,
    [KnowledgeAreaTypeID] INT NULL,
    [SubAreaTypeID] INT NULL,
    [AreaTypeID] INT NULL,
    [VolumeCount] INT NULL,
    [ParticipationTypeID] INT NULL,
    [PublicationDate] DATE NULL,
    [UTAffiliation] BIT NULL,
    [UTASponsorship] BIT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [bookTypeID] INT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Cantons]') IS NULL
CREATE TABLE [HR].[tbl_Cantons] (
    [CantonID] NVARCHAR(10) NOT NULL,
    [ProvinceID] NVARCHAR(10) NOT NULL,
    [CantonName] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_CatastrophicIllnesses]') IS NULL
CREATE TABLE [HR].[tbl_CatastrophicIllnesses] (
    [IllnessID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [Illness] NVARCHAR(150) NOT NULL,
    [IESSNumber] NVARCHAR(50) NULL,
    [SubstituteName] NVARCHAR(100) NULL,
    [IllnessTypeID] INT NOT NULL,
    [CertificateNumber] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_contract_status_history]') IS NULL
CREATE TABLE [HR].[tbl_contract_status_history] (
    [HistoryID] INT IDENTITY(1,1) NOT NULL,
    [ContractID] INT NOT NULL,
    [StatusTypeID] INT NOT NULL,
    [Comment] NVARCHAR(500) NULL,
    [ChangedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [ChangedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_contract_status_transitions]') IS NULL
CREATE TABLE [HR].[tbl_contract_status_transitions] (
    [TransitionID] INT IDENTITY(1,1) NOT NULL,
    [FromStatusTypeID] INT NOT NULL,
    [ToStatusTypeID] INT NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (sysutcdatetime()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_contract_type]') IS NULL
CREATE TABLE [HR].[tbl_contract_type] (
    [ContractTypeID] INT IDENTITY(1,1) NOT NULL,
    [PersonalContractTypeID] INT NULL,
    [name] NVARCHAR(100) NOT NULL,
    [description] NVARCHAR(150) NULL,
    [Status] VARCHAR(1) NOT NULL,
    [ContractText] NVARCHAR(MAX) NULL,
    [ContractCode] NVARCHAR(30) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [DocumentTemplateTypeID] INT NULL,
    [DefaultTemplateID] INT NULL,
    -- Plantilla alterna a usar cuando el contrato se firma por delegación (Contracts.IsDelegation = 1).
    [DelegationTemplateId] INT NULL,
    [NumberingPrefix] NVARCHAR(50) NULL,
    [NumberingYear] INT DEFAULT (datepart(year,getdate())) NOT NULL,
    [NumberingLastSequence] INT DEFAULT ((0)) NOT NULL,
    [RequiresAdUserCreation] BIT DEFAULT ((0)) NOT NULL,
    [RequiresAdUserDisable] BIT DEFAULT ((0)) NOT NULL,
    [RequiresAdGroupAssignment] BIT DEFAULT ((0)) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_contractRequest]') IS NULL
CREATE TABLE [HR].[tbl_contractRequest] (
    [RequestID] INT IDENTITY(1,1) NOT NULL,
    [WorkModalityID] INT NULL,
    [DepartmentID] INT NULL,
    [NumberOfPeopleToHire] INT DEFAULT ((0)) NOT NULL,
    [NumberHour] DECIMAL(12,2) DEFAULT ((0)) NOT NULL,
    [TotalPeopleHired] INT DEFAULT ((0)) NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [CreatedBy] INT NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [Status] INT NULL,
    [Observation] NVARCHAR(1000) NULL,
    [StartDate] DATE NULL,
    [EndDate] DATE NULL,
    [PendingCorrectionReason] NVARCHAR(1000) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_contractRequestPerson]') IS NULL
CREATE TABLE [HR].[tbl_contractRequestPerson] (
    [RequestPersonID] INT IDENTITY(1,1) NOT NULL,
    [RequestID] INT NOT NULL,
    [PersonID] INT NULL,
    [RequestPersonType] INT NOT NULL,
    [JobID] INT NOT NULL,
    [StartDate] DATE NULL,
    [EndDate] DATE NULL,
    [WeeklyClassHours] DECIMAL(12,2) NULL,
    [HourValue] DECIMAL(12,4) NULL,
    [MonthsPeriod] DECIMAL(12,4) NULL,
    [RMU] DECIMAL(12,2) NULL,
    [RMUPeriod] DECIMAL(12,2) NULL,
    [EntrySourceID] INT NOT NULL,
    [IsHired] BIT DEFAULT ((0)) NOT NULL,
    [ContractID] INT NULL,
    [HiredAt] DATETIME2 NULL,
    [HiredBy] INT NULL,
    [Observation] NVARCHAR(1000) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [InactiveAt] DATETIME2 NULL,
    [InactiveBy] INT NULL,
    [InactiveReason] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [Status] INT NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Contracts]') IS NULL
CREATE TABLE [HR].[tbl_Contracts] (
    [ContractID] INT IDENTITY(1,1) NOT NULL,
    [CertificationID] INT NULL,
    [ParentID] INT NULL,
    [ContractCode] NVARCHAR(30) NOT NULL,
    [PersonID] INT NOT NULL,
    [ContractTypeID] INT NOT NULL,
    [JobID] INT NULL,
    [startdate] DATETIME2 NOT NULL,
    [enddate] DATETIME2 NOT NULL,
    [ContractFileName] NVARCHAR(200) NULL,
    [ContractFilepath] NVARCHAR(MAX) NULL,
    [Status] INT DEFAULT ((0)) NOT NULL,
    [ContractDescription] NVARCHAR(MAX) NULL,
    [DepartmentID] INT NOT NULL,
    [authorizationdate] DATETIME2 NULL,
    [ResignationFileName] NVARCHAR(150) NULL,
    [ResignationFilepath] NVARCHAR(250) NULL,
    [ResignationCode] NVARCHAR(20) NULL,
    [RegResignationdate] DATETIME2 NULL,
    [Resignationdate] DATETIME2 NULL,
    [Cancelreason] NVARCHAR(250) NULL,
    [CancelFilename] NVARCHAR(150) NULL,
    [CancelFilepath] NVARCHAR(250) NULL,
    [CancelCode] NVARCHAR(20) NULL,
    [registrationdate_anul_con] DATETIME2 NULL,
    [nationality] NVARCHAR(100) NULL,
    [visa] NVARCHAR(150) NULL,
    [consulate] NVARCHAR(150) NULL,
    [work_of] NVARCHAR(150) NULL,
    [InicialContent] NVARCHAR(MAX) NULL,
    [ResolucionContent] NVARCHAR(MAX) NULL,
    [relationshiptype] INT NULL,
    [relationship] NVARCHAR(500) NULL,
    [competition] NVARCHAR(800) NULL,
    [competitionDate] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [GeneratedDocumentID] INT NULL,
    [TemplateVersionUsed] INT NULL,
    [IsDocumentFrozen] BIT DEFAULT ((0)) NOT NULL,
    [SignedDocumentStoredFileId] INT NULL,
    [AuthorityNominatorId] INT NULL,
    [DthDirectorId] INT NULL,
    -- Indica si la firma se realiza por delegación (AuthorityNominatorId = el delegado) o
    -- directamente por la máxima autoridad (Rector/a). Punto de filtro explícito.
    [IsDelegation] BIT DEFAULT ((0)) NOT NULL,
    [LaborRegimeID] INT NULL,
    [WorkModalityID] INT NULL,
    [ContractedHours] DECIMAL(5,2) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Countries]') IS NULL
CREATE TABLE [HR].[tbl_Countries] (
    [CountryID] NVARCHAR(10) NOT NULL,
    [CountryName] NVARCHAR(100) NOT NULL,
    [Nationality] NVARCHAR(100) NULL,
    [NationalityCode] NVARCHAR(5) NULL,
    [AuxSIITH] NVARCHAR(5) NULL,
    [AuxCEAACES] NVARCHAR(5) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Degrees]') IS NULL
CREATE TABLE [HR].[tbl_Degrees] (
    [DegreeID] INT IDENTITY(1,1) NOT NULL,
    [Description] NVARCHAR(200) NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_DepartmentAuthorities]') IS NULL
CREATE TABLE [HR].[tbl_DepartmentAuthorities] (
    [AuthorityID] INT IDENTITY(1,1) NOT NULL,
    [DepartmentID] INT NOT NULL,
    [EmployeeID] INT NOT NULL,
    [AuthorityTypeID] INT NOT NULL,
    [JobID] INT NULL,
    [Denomination] NVARCHAR(200) NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NULL,
    [ResolutionCode] NVARCHAR(100) NULL,
    [Notes] NVARCHAR(500) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Departments]') IS NULL
CREATE TABLE [HR].[tbl_Departments] (
    [DepartmentID] INT IDENTITY(1,1) NOT NULL,
    [ParentID] INT NULL,
    [Code] NVARCHAR(20) NOT NULL,
    [Name] NVARCHAR(120) NOT NULL,
    [ShortName] NVARCHAR(50) NULL,
    [DepartmentType] INT NOT NULL,
    [Email] NVARCHAR(100) NULL,
    [Phone] NVARCHAR(20) NULL,
    [Location] NVARCHAR(200) NULL,
    [DeanDirector] INT NULL,
    [BudgetCode] NVARCHAR(30) NULL,
    [Dlevel] INT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL,
    [DepartmentScope] INT NULL,
    -- Rol institucional crítico para resolución de firmas (RECTORADO, FINANCE, HUMAN_RESOURCE, etc.)
    -- Ver HR.ref_Types con Category = 'DEPARTMENT_INSTITUTIONAL_ROLE'. FK en 02_constraints.sql.
    [InstitutionalRoleTypeId] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[TBL_DirectoryParameters]') IS NULL
CREATE TABLE [HR].[TBL_DirectoryParameters] (
    [DirectoryID] INT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(50) NOT NULL,
    [PhysicalPath] NVARCHAR(MAX) NOT NULL,
    [RelativePath] NVARCHAR(MAX) NULL,
    [Description] NVARCHAR(255) NULL,
    [Extension] NVARCHAR(MAX) NULL,
    [MaxSizeMB] INT NULL,
    [Status] BIT DEFAULT ((1)) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_DocumentTemplateFields]') IS NULL
CREATE TABLE [HR].[tbl_DocumentTemplateFields] (
    [FieldID] INT IDENTITY(1,1) NOT NULL,
    [TemplateID] INT NOT NULL,
    [FieldName] NVARCHAR(100) NOT NULL,
    [Label] NVARCHAR(150) NOT NULL,
    [SourceType] NVARCHAR(20) DEFAULT ('SYSTEM') NOT NULL,
    [SourceProperty] NVARCHAR(200) NULL,
    [DataType] NVARCHAR(20) DEFAULT ('TEXT') NOT NULL,
    [FormatPattern] NVARCHAR(50) NULL,
    [DefaultValue] NVARCHAR(500) NULL,
    [IsRequired] BIT DEFAULT ((0)) NOT NULL,
    [IsEditable] BIT DEFAULT ((0)) NOT NULL,
    [SortOrder] INT DEFAULT ((0)) NOT NULL,
    [HelpText] NVARCHAR(300) NULL,
    [CreatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_DocumentTemplates]') IS NULL
CREATE TABLE [HR].[tbl_DocumentTemplates] (
    [TemplateID] INT IDENTITY(1,1) NOT NULL,
    -- TemplateCode se repite entre varias filas: cada fila es una versión (Draft/Published/Archived)
    -- de la misma familia de plantilla. La vigente es la única que puede estar Published
    -- (ver índice único filtrado UX_DocumentTemplates_TemplateCode_Published).
    [TemplateCode] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [TemplateType] NVARCHAR(50) NOT NULL,
    [Version] NVARCHAR(10) DEFAULT ('1.0') NOT NULL,
    [LayoutType] NVARCHAR(20) DEFAULT ('FLOW_TEXT') NOT NULL,
    [Status] NVARCHAR(20) DEFAULT ('DRAFT') NOT NULL,
    [HtmlContent] NVARCHAR(MAX) NOT NULL,
    [CssStyles] NVARCHAR(MAX) NULL,
    [MetaJson] NVARCHAR(MAX) NULL,
    [RequiresSignature] BIT DEFAULT ((0)) NOT NULL,
    [RequiresApproval] BIT DEFAULT ((0)) NOT NULL,
    [CreatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- Garantiza por base de datos que nunca existan dos versiones Published del mismo TemplateCode
-- al mismo tiempo, sin importar qué código de aplicación toque el Status.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DocumentTemplates_TemplateCode_Published')
CREATE UNIQUE INDEX UX_DocumentTemplates_TemplateCode_Published
    ON [HR].[tbl_DocumentTemplates]([TemplateCode])
    WHERE [Status] = 'PUBLISHED';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentTemplates_TemplateCode')
CREATE INDEX IX_DocumentTemplates_TemplateCode
    ON [HR].[tbl_DocumentTemplates]([TemplateCode]);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_EducationLevels]') IS NULL
CREATE TABLE [HR].[tbl_EducationLevels] (
    [EducationID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [EducationLevelTypeID] INT NOT NULL,
    [InstitutionID] INT NOT NULL,
    [Title] NVARCHAR(150) NOT NULL,
    [Specialty] NVARCHAR(100) NULL,
    [StartDate] DATE NULL,
    [EndDate] DATE NULL,
    [Grade] NVARCHAR(50) NULL,
    [Location] NVARCHAR(100) NULL,
    [Score] DECIMAL(5,2) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_EmailLayouts]') IS NULL
CREATE TABLE [HR].[tbl_EmailLayouts] (
    [EmailLayoutID] INT IDENTITY(1,1) NOT NULL,
    [Slug] NVARCHAR(150) NOT NULL,
    [HeaderHtml] NVARCHAR(MAX) NULL,
    [FooterHtml] NVARCHAR(MAX) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_EmailLogAttachments]') IS NULL
CREATE TABLE [HR].[tbl_EmailLogAttachments] (
    [EmailLogAttachmentID] INT IDENTITY(1,1) NOT NULL,
    [EmailLogID] INT NOT NULL,
    [StoredFileGuid] UNIQUEIDENTIFIER NOT NULL,
    [FileName] NVARCHAR(260) NULL,
    [ContentType] NVARCHAR(100) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_EmailLogs]') IS NULL
CREATE TABLE [HR].[tbl_EmailLogs] (
    [EmailLogID] INT IDENTITY(1,1) NOT NULL,
    [Recipient] NVARCHAR(320) NOT NULL,
    [Subject] NVARCHAR(255) NOT NULL,
    [BodyRendered] NVARCHAR(MAX) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL,
    [SentAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_EmergencyContacts]') IS NULL
CREATE TABLE [HR].[tbl_EmergencyContacts] (
    [ContactID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [Identification] NVARCHAR(20) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [RelationshipTypeID] INT NOT NULL,
    [Address] NVARCHAR(255) NULL,
    [Phone] NVARCHAR(30) NULL,
    [Mobile] NVARCHAR(30) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_EmployeeAvailabilityBlocks]') IS NULL
CREATE TABLE [HR].[tbl_EmployeeAvailabilityBlocks] (
    [BlockID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [SourceTypeID] INT NOT NULL,
    [SourceTable] SYSNAME NULL,
    [SourceID] NVARCHAR(128) NULL,
    [StartDateTime] DATETIME2 NOT NULL,
    [EndDateTime] DATETIME2 NOT NULL,
    [StatusTypeID] INT NOT NULL,
    [Reason] NVARCHAR(500) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Employees]') IS NULL
CREATE TABLE [HR].[tbl_Employees] (
    [EmployeeID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [EmployeeType] INT NULL,
    [DepartmentID] INT NULL,
    [ImmediateBossID] INT NULL,
    [HireDate] DATE NOT NULL,
    [Email] NVARCHAR(150) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [JobID] INT NULL,
    [LaborRegimeID] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_EmployeeSchedules]') IS NULL
CREATE TABLE [HR].[tbl_EmployeeSchedules] (
    [EmpScheduleID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [ScheduleID] INT NOT NULL,
    [ValidFrom] DATE NOT NULL,
    [ValidTo] DATE NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_FamilyBurden]') IS NULL
CREATE TABLE [HR].[tbl_FamilyBurden] (
    [BurdenID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [DependentID] NVARCHAR(20) NOT NULL,
    [IdentificationTypeID] INT NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [BirthDate] DATE NOT NULL,
    [DisabilityTypeID] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_FinancialCertification]') IS NULL
CREATE TABLE [HR].[tbl_FinancialCertification] (
    [CertificationID] INT IDENTITY(1,1) NOT NULL,
    [RequestID] INT NULL,
    [CertCode] NVARCHAR(100) NOT NULL,
    [CertNumber] NVARCHAR(100) NULL,
    [budget] NVARCHAR(100) NULL,
    [CertBudgetDate] DATETIME2 NULL,
    [rmu_hour] DECIMAL(12,2) NULL,
    [rmu_con] DECIMAL(12,2) NULL,
    [Status] INT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [CreatedBy] INT NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [filename] NVARCHAR(150) NULL,
    [filepath] NVARCHAR(MAX) NULL,
    [RejectionReason] NVARCHAR(1000) NULL,
    [RejectedAt] DATETIME2 NULL,
    [RejectedBy] INT NULL,
    [RejectionTypeID] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_FinancialCertificationRejectionHistory]') IS NULL
CREATE TABLE [HR].[tbl_FinancialCertificationRejectionHistory] (
    [RejectionHistoryID] INT IDENTITY(1,1) NOT NULL,
    [CertificationID] INT NOT NULL,
    [RequestID] INT NOT NULL,
    [RejectionTypeID] INT NOT NULL,
    [RejectionReason] NVARCHAR(1000) NOT NULL,
    [PreviousCertificationStatus] INT NULL,
    [NewCertificationStatus] INT NOT NULL,
    [PreviousRequestStatus] INT NULL,
    [NewRequestStatus] INT NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GeneratedDocumentFields]') IS NULL
CREATE TABLE [HR].[tbl_GeneratedDocumentFields] (
    [DocumentFieldID] INT IDENTITY(1,1) NOT NULL,
    [DocumentID] INT NOT NULL,
    [FieldName] NVARCHAR(100) NOT NULL,
    [FieldValue] NVARCHAR(MAX) NULL,
    [SourceType] NVARCHAR(20) DEFAULT ('SYSTEM') NOT NULL,
    [WasOverridden] BIT DEFAULT ((0)) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GeneratedDocuments]') IS NULL
CREATE TABLE [HR].[tbl_GeneratedDocuments] (
    [DocumentID] INT IDENTITY(1,1) NOT NULL,
    [TemplateID] INT NOT NULL,
    [EmployeeID] INT NULL,
    [EntityType] NVARCHAR(30) NOT NULL,
    [EntityId] INT NULL,
    [DocumentNumber] NVARCHAR(50) NULL,
    [FileName] NVARCHAR(255) NOT NULL,
    [StoredFileID] INT NULL,
    [Status] NVARCHAR(20) DEFAULT ('DRAFT') NOT NULL,
    [Notes] NVARCHAR(1000) NULL,
    [IsSigned] BIT DEFAULT ((0)) NOT NULL,
    [SignedAt] DATETIME2 NULL,
    [SignedBy] INT NULL,
    [IsApproved] BIT DEFAULT ((0)) NOT NULL,
    [ApprovedAt] DATETIME2 NULL,
    [ApprovedBy] INT NULL,
    [CreatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [TemplateVersion] NVARCHAR(10) NULL,
    [HtmlSnapshot] NVARCHAR(MAX) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardAssignmentValidations]') IS NULL
CREATE TABLE [HR].[tbl_GuardAssignmentValidations] (
    [ValidationID] BIGINT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [PlanningID] INT NULL,
    [ShiftChangeID] INT NULL,
    [ValidationTypeID] INT NOT NULL,
    [ResultTypeID] INT NOT NULL,
    [SeverityTypeID] INT NOT NULL,
    [ValidationDate] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [Message] NVARCHAR(1000) NOT NULL,
    [Details] NVARCHAR(MAX) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardEmployeeSpecialRules]') IS NULL
CREATE TABLE [HR].[tbl_GuardEmployeeSpecialRules] (
    [SpecialRuleId] INT IDENTITY(1,1) NOT NULL,
    [EmployeeId] INT NOT NULL,
    [FixedLocationId] INT NULL,
    [FixedScheduleId] INT NULL,
    [NoNightShift] BIT DEFAULT ((0)) NOT NULL,
    [OnlyWeekDays] BIT DEFAULT ((0)) NOT NULL,
    [WeekendPriority] BIT DEFAULT ((0)) NOT NULL,
    [NightPriority] BIT DEFAULT ((0)) NOT NULL,
    [Reason] NVARCHAR(500) NULL,
    [ValidFrom] DATE NOT NULL,
    [ValidTo] DATE NULL,
    [RequiresApproval] BIT DEFAULT ((0)) NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardGroupRotationPatterns]') IS NULL
CREATE TABLE [HR].[tbl_GuardGroupRotationPatterns] (
    [GroupPatternID] INT IDENTITY(1,1) NOT NULL,
    [GroupID] INT NOT NULL,
    [PatternID] INT NOT NULL,
    [StartCycleDate] DATE NOT NULL,
    [ValidFrom] DATE NOT NULL,
    [ValidTo] DATE NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [Notes] NVARCHAR(500) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardLocationRotationAssignments]') IS NULL
CREATE TABLE [HR].[tbl_GuardLocationRotationAssignments] (
    [LocationRotationAssignmentId] INT IDENTITY(1,1) NOT NULL,
    [LocationRotationPeriodId] INT NOT NULL,
    [GroupId] INT NULL,
    [EmployeeId] INT NULL,
    [LocationId] INT NOT NULL,
    [PriorityTypeId] INT NULL,
    [IsFixedLocation] BIT DEFAULT ((0)) NOT NULL,
    [IsFixedSchedule] BIT DEFAULT ((0)) NOT NULL,
    [Notes] NVARCHAR(500) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardLocationRotationPeriods]') IS NULL
CREATE TABLE [HR].[tbl_GuardLocationRotationPeriods] (
    [LocationRotationPeriodId] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [Notes] NVARCHAR(500) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardRotationGroupEmployees]') IS NULL
CREATE TABLE [HR].[tbl_GuardRotationGroupEmployees] (
    [GroupEmployeeID] INT IDENTITY(1,1) NOT NULL,
    [GroupID] INT NOT NULL,
    [EmployeeID] INT NOT NULL,
    [ValidFrom] DATE NOT NULL,
    [ValidTo] DATE NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [Notes] NVARCHAR(500) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardRotationGroups]') IS NULL
CREATE TABLE [HR].[tbl_GuardRotationGroups] (
    [GroupID] INT IDENTITY(1,1) NOT NULL,
    [GroupCode] NVARCHAR(30) NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [ParentGroupId] INT NULL,
    [GroupLevelTypeId] INT NULL,
    [ColorCode] NVARCHAR(20) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardServiceLocations]') IS NULL
CREATE TABLE [HR].[tbl_GuardServiceLocations] (
    [LocationID] INT IDENTITY(1,1) NOT NULL,
    [ParentLocationID] INT NULL,
    [RootLocationID] INT NULL,
    [LocationTypeID] INT NOT NULL,
    [LocationCode] NVARCHAR(30) NULL,
    [LocationName] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [LocationPath] NVARCHAR(900) NULL,
    [Level] INT DEFAULT ((0)) NOT NULL,
    [RequiresCoverage] BIT DEFAULT ((0)) NOT NULL,
    [IsAssignable] BIT DEFAULT ((0)) NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardSettings]') IS NULL
CREATE TABLE [HR].[tbl_GuardSettings] (
    [SettingKey] NVARCHAR(100) NOT NULL,
    [SettingValue] NVARCHAR(500) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 DEFAULT (getdate()) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardShiftChanges]') IS NULL
CREATE TABLE [HR].[tbl_GuardShiftChanges] (
    [ShiftChangeID] INT IDENTITY(1,1) NOT NULL,
    [PlanningID] INT NOT NULL,
    [OriginalEmployeeID] INT NOT NULL,
    [ReplacementEmployeeID] INT NULL,
    [OriginalScheduleID] INT NOT NULL,
    [NewScheduleID] INT NULL,
    [ChangeTypeID] INT NOT NULL,
    [StatusTypeID] INT NOT NULL,
    [IsActiveForAttendance] BIT DEFAULT ((0)) NOT NULL,
    [Reason] NVARCHAR(1000) NOT NULL,
    [RequestedBy] INT NULL,
    [RequestedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [ApprovedBy] INT NULL,
    [ApprovedAt] DATETIME2 NULL,
    [RejectionReason] NVARCHAR(500) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardShiftCoverageRequirements]') IS NULL
CREATE TABLE [HR].[tbl_GuardShiftCoverageRequirements] (
    [RequirementID] INT IDENTITY(1,1) NOT NULL,
    [LocationID] INT NOT NULL,
    [ScheduleID] INT NOT NULL,
    [DayOfWeek] TINYINT NOT NULL,
    [RequiredGuards] INT DEFAULT ((1)) NOT NULL,
    [ValidFrom] DATE NOT NULL,
    [ValidTo] DATE NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [Notes] NVARCHAR(500) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardShiftPlanning]') IS NULL
CREATE TABLE [HR].[tbl_GuardShiftPlanning] (
    [PlanningID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [GroupID] INT NULL,
    [LocationID] INT NOT NULL,
    [WorkDate] DATE NOT NULL,
    [ScheduleID] INT NOT NULL,
    [PlanningSourceTypeID] INT NOT NULL,
    [StatusTypeID] INT NOT NULL,
    [IsAutoGenerated] BIT DEFAULT ((1)) NOT NULL,
    [IsActiveForAssignment] BIT DEFAULT ((1)) NOT NULL,
    [Notes] NVARCHAR(500) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardVacationPlans]') IS NULL
CREATE TABLE [HR].[tbl_GuardVacationPlans] (
    [GuardVacationPlanId] INT IDENTITY(1,1) NOT NULL,
    [EmployeeId] INT NOT NULL,
    [VacationYear] INT NOT NULL,
    [PlannedStartDate] DATE NOT NULL,
    [PlannedEndDate] DATE NOT NULL,
    [StatusTypeId] INT NOT NULL,
    [DirectionApprovedBy] INT NULL,
    [DirectionApprovedAt] DATETIME2 NULL,
    [SubmittedToDirectionBy] INT NULL,
    [SubmittedToDirectionAt] DATETIME2 NULL,
    [Notes] NVARCHAR(1000) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- Agregar columnas de envío a dirección si no existen (idempotente)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardVacationPlans]') AND name = 'SubmittedToDirectionBy')
    ALTER TABLE [HR].[tbl_GuardVacationPlans] ADD [SubmittedToDirectionBy] INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardVacationPlans]') AND name = 'SubmittedToDirectionAt')
    ALTER TABLE [HR].[tbl_GuardVacationPlans] ADD [SubmittedToDirectionAt] DATETIME2 NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_GuardVacationRequests]') IS NULL
CREATE TABLE [HR].[tbl_GuardVacationRequests] (
    [GuardVacationRequestId] INT IDENTITY(1,1) NOT NULL,
    [EmployeeId] INT NOT NULL,
    [GuardVacationPlanId] INT NULL,
    [VacationId] INT NULL,
    [RequestTypeId] INT NOT NULL,
    [OriginalStartDate] DATE NOT NULL,
    [OriginalEndDate] DATE NOT NULL,
    [RequestedStartDate] DATE NULL,
    [RequestedEndDate] DATE NULL,
    [SourceYear] INT NOT NULL,
    [TargetYear] INT NULL,
    [Reason] NVARCHAR(1000) NOT NULL,
    [StatusTypeId] INT NOT NULL,
    [RequestedBy] INT NULL,
    [RequestedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [DirectionApprovedBy] INT NULL,
    [DirectionApprovedAt] DATETIME2 NULL,
    [RejectedBy] INT NULL,
    [RejectedAt] DATETIME2 NULL,
    [RejectionReason] NVARCHAR(500) NULL,
    [SubmittedToDirectionBy] INT NULL,
    [SubmittedToDirectionAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- Agregar columnas de envío a dirección si no existen (idempotente)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardVacationRequests]') AND name = 'SubmittedToDirectionBy')
    ALTER TABLE [HR].[tbl_GuardVacationRequests] ADD [SubmittedToDirectionBy] INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardVacationRequests]') AND name = 'SubmittedToDirectionAt')
    ALTER TABLE [HR].[tbl_GuardVacationRequests] ADD [SubmittedToDirectionAt] DATETIME2 NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Holidays]') IS NULL
CREATE TABLE [HR].[tbl_Holidays] (
    [HolidayID] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [HolidayDate] DATE NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [Description] NVARCHAR(255) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Institutions]') IS NULL
CREATE TABLE [HR].[tbl_Institutions] (
    [InstitutionID] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [InstitutionTypeID] INT NOT NULL,
    [CountryID] NVARCHAR(10) NOT NULL,
    [ProvinceID] NVARCHAR(10) NOT NULL,
    [CantonID] NVARCHAR(10) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_JobActivities]') IS NULL
CREATE TABLE [HR].[tbl_JobActivities] (
    [ActivitiesID] INT NOT NULL,
    [JobID] INT NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL,
    [ActivityDescription] NVARCHAR(1000) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_jobs]') IS NULL
CREATE TABLE [HR].[tbl_jobs] (
    [JobID] INT IDENTITY(1,1) NOT NULL,
    [JobTypeID] INT NULL,
    [GroupID] INT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL,
    [LaborRegimeID] INT NULL
    [Description] NVARCHAR(500) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_KnowledgeArea]') IS NULL
CREATE TABLE [HR].[tbl_KnowledgeArea] (
    [id] INT IDENTITY(1,1) NOT NULL,
    [code] VARCHAR(10) NOT NULL,
    [name] VARCHAR(200) NOT NULL,
    [parent_id] INT NULL,
    [levels] INT NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Occupational_Groups]') IS NULL
CREATE TABLE [HR].[tbl_Occupational_Groups] (
    [GroupID] INT IDENTITY(1,1) NOT NULL,
    [Description] NVARCHAR(200) NOT NULL,
    [RMU] DECIMAL(10,2) NOT NULL,
    [DegreeID] INT NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Overtime]') IS NULL
CREATE TABLE [HR].[tbl_Overtime] (
    [OvertimeID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [WorkDate] DATE NOT NULL,
    [OvertimeType] NVARCHAR(50) NOT NULL,
    [Hours] DECIMAL(5,2) NOT NULL,
    [Status] NVARCHAR(20) DEFAULT ('EXECUTED') NOT NULL,
    [ApprovedBy] INT NULL,
    [SecondApprover] INT NULL,
    [Factor] DECIMAL(5,2) NOT NULL,
    [ActualHours] DECIMAL(5,2) DEFAULT ((0)) NOT NULL,
    [PaymentAmount] DECIMAL(12,2) DEFAULT ((0)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- 2026-07-06 (punto 6): trazabilidad al plan de origen. Nullable porque filas
-- históricas/manuales sin plan asociado deben seguir siendo válidas. Cuando
-- hubo más de un plan de Overtime el mismo día, representa el plan "ganador"
-- del desempate por Factor, no todos los que contribuyeron ese día.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_Overtime]') AND name = 'PlanEmployeeID'
)
    ALTER TABLE [HR].[tbl_Overtime] ADD [PlanEmployeeID] INT NULL;
GO

-- 2026-07-06 (Fase 3, propuesta multi-régimen): régimen laboral que originó
-- la línea. Siempre 57 (LOSEP) para filas escritas por el pipeline de
-- asistencia, porque solo ese régimen genera horas extra (confirmado). Nullable
-- por filas históricas previas a este cambio.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_Overtime]') AND name = 'LaborRegimeId'
)
    ALTER TABLE [HR].[tbl_Overtime] ADD [LaborRegimeId] INT NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_OvertimeConfig]') IS NULL
CREATE TABLE [HR].[tbl_OvertimeConfig] (
    [OvertimeType] NVARCHAR(50) NOT NULL,
    [Factor] DECIMAL(5,2) NOT NULL,
    [Description] NVARCHAR(200) NULL
);
GO

-- Tope opcional de horas extra por tipo (Fase 4, punto 11). NULL = sin tope activo.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_OvertimeConfig]') AND name = 'MaxDailyMinutes'
)
    ALTER TABLE [HR].[tbl_OvertimeConfig] ADD [MaxDailyMinutes] INT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_OvertimeConfig]') AND name = 'MaxWeeklyMinutes'
)
    ALTER TABLE [HR].[tbl_OvertimeConfig] ADD [MaxWeeklyMinutes] INT NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[TBL_PARAMETERS]') IS NULL
CREATE TABLE [HR].[TBL_PARAMETERS] (
    [ParameterID] INT IDENTITY(1,1) NOT NULL,
    [name] NVARCHAR(100) NOT NULL,
    [Pvalues] NVARCHAR(MAX) NULL,
    [Description] NVARCHAR(255) NULL,
    [DataType] NVARCHAR(20) NULL,
    [IsActive] BIT DEFAULT ((1)) NULL,
    [CreatedAt] DATETIME DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] NVARCHAR(50) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Payroll]') IS NULL
CREATE TABLE [HR].[tbl_Payroll] (
    [PayrollID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [Period] CHAR(7) NOT NULL,
    [BaseSalary] DECIMAL(12,2) NOT NULL,
    [Status] NVARCHAR(15) DEFAULT ('Pending') NOT NULL,
    [PaymentDate] DATE NULL,
    [BankAccount] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_PayrollLines]') IS NULL
CREATE TABLE [HR].[tbl_PayrollLines] (
    [PayrollLineID] INT IDENTITY(1,1) NOT NULL,
    [PayrollID] INT NOT NULL,
    [LineType] NVARCHAR(20) NOT NULL,
    [Concept] NVARCHAR(120) NOT NULL,
    [Quantity] DECIMAL(10,2) DEFAULT ((1)) NOT NULL,
    [UnitValue] DECIMAL(12,2) DEFAULT ((0)) NOT NULL,
    [Notes] NVARCHAR(300) NULL
);
GO

-- 2026-07-06 (Fase 3, propuesta multi-régimen): régimen laboral que originó la
-- línea (hoy solo poblado por horas extra, siempre 57=LOSEP). Descuentos y
-- subsidios todavía colapsan al régimen principal, no separan por línea, así
-- que quedan NULL por ahora.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_PayrollLines]') AND name = 'LaborRegimeId'
)
    ALTER TABLE [HR].[tbl_PayrollLines] ADD [LaborRegimeId] INT NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_People]') IS NULL
CREATE TABLE [HR].[tbl_People] (
    [PersonID] INT IDENTITY(1,1) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [IdentType] INT NULL,
    [IDCard] NVARCHAR(20) NOT NULL,
    [Email] NVARCHAR(150) NOT NULL,
    [Phone] NVARCHAR(30) NULL,
    [BirthDate] DATE NULL,
    [Sex] INT NULL,
    [Gender] INT NULL,
    [Disability] NVARCHAR(200) NULL,
    [Address] NVARCHAR(255) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [MaritalStatusTypeID] INT NULL,
    [MilitaryCard] NVARCHAR(50) NULL,
    [MotherName] NVARCHAR(100) NULL,
    [FatherName] NVARCHAR(100) NULL,
    [CountryID] NVARCHAR(10) NULL,
    [ProvinceID] NVARCHAR(10) NULL,
    [CantonID] NVARCHAR(10) NULL,
    [YearsOfResidence] INT NULL,
    [EthnicityTypeID] INT NULL,
    [BloodTypeTypeID] INT NULL,
    [SpecialNeedsTypeID] INT NULL,
    [DisabilityPercentage] DECIMAL(5,2) NULL,
    [CONADISCard] NVARCHAR(50) NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Permissions]') IS NULL
CREATE TABLE [HR].[tbl_Permissions] (
    [PermissionID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [PermissionTypeID] INT NOT NULL,
    [StartDate] DATETIME2 NOT NULL,
    [EndDate] DATETIME2 NOT NULL,
    [ChargedToVacation] BIT DEFAULT ((0)) NOT NULL,
    [ApprovedBy] INT NULL,
    [Justification] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [Status] NVARCHAR(20) DEFAULT ('Pending') NOT NULL,
    [VacationID] INT NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [ApprovedAt] DATETIME2 NULL,
    [HourTaken] DECIMAL(5,2) NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_PermissionTypes]') IS NULL
CREATE TABLE [HR].[tbl_PermissionTypes] (
    [TypeID] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(80) NOT NULL,
    [DeductsFromVacation] BIT DEFAULT ((0)) NOT NULL,
    [RequiresApproval] BIT DEFAULT ((1)) NOT NULL,
    [MaxDays] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [AttachedFile] BIT DEFAULT ((1)) NULL,
    [LeadTimeHours] INT DEFAULT ((0)) NULL,
    [IsMedical] BIT DEFAULT ((0)) NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [ContractTypeID] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_personnel_action_type]') IS NULL
CREATE TABLE [HR].[tbl_personnel_action_type] (
    [PersonnelActionTypeId] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Code] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(300) NULL,
    [NumberingPrefix] NVARCHAR(30) NOT NULL,
    [NumberingYear] INT DEFAULT (datepart(year,getdate())) NOT NULL,
    [NumberingLastSequence] INT DEFAULT ((0)) NOT NULL,
    -- FK a la plantilla documental (DocumentTemplates.TemplateID) usada por defecto para este
    -- tipo de acción. Antes era un código de texto libre (TemplateCode) que en realidad guardaba
    -- el TemplateType, no un código de plantilla específico — se convirtió a FK real.
    [DefaultTemplateId] INT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [RequiresAdUserCreation] BIT DEFAULT ((0)) NOT NULL,
    [RequiresAdUserDisable] BIT DEFAULT ((0)) NOT NULL,
    [RequiresAdGroupAssignment] BIT DEFAULT ((0)) NOT NULL
);
GO

-- 2026-07-06 (propuesta VIGENTE en Acciones de Personal): marca qué tipos
-- participan en la cadena de "acción vigente" del empleado (Nombramiento,
-- Traslado, Encargo, Cambio de Sueldo, Asistencia/Horario). Los que no
-- participan (Comisión, Licencia, Sanción, Vulnerabilidad, Vacaciones) siguen
-- yendo directo FIRMADO_CARGADO → FINALIZADO, sin pasar por VIGENTE.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_personnel_action_type]') AND name = 'ReachesVigente'
)
    ALTER TABLE [HR].[tbl_personnel_action_type] ADD [ReachesVigente] BIT NOT NULL DEFAULT ((0));
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_PersonnelActions]') IS NULL
CREATE TABLE [HR].[tbl_PersonnelActions] (
    [ActionID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NULL,
    [ActionTypeID] INT NOT NULL,
    [ContractID] INT NULL,
    [ActionNumber] NVARCHAR(50) NULL,
    [ActionDate] DATE NOT NULL,
    [EffectiveDate] DATE NULL,
    [EndDate] DATE NULL,
    [OriginDepartmentId] INT NULL,
    [OriginJobId] INT NULL,
    [OriginBudgetCode] NVARCHAR(50) NULL,
    [DestinationDepartmentId] INT NULL,
    [DestinationJobId] INT NULL,
    [DestinationBudgetCode] NVARCHAR(50) NULL,
    [PreviousRmu] DECIMAL(10,2) NULL,
    [NewRmu] DECIMAL(10,2) NULL,
    [LegalBasis] NVARCHAR(500) NULL,
    [Reason] NVARCHAR(1000) NULL,
    [Observations] NVARCHAR(1000) NULL,
    [Status] NVARCHAR(20) DEFAULT ('DRAFT') NOT NULL,
    [GeneratedDocumentID] INT NULL,
    [MovementID] INT NULL,
    [CreatedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [StatusTypeId] INT NULL,
    [SignedDocumentStoredFileId] INT NULL,
    [DthDirectorID] INT NULL,
    [AuthorityNominatorID] INT NULL,
    [ElaboratorID] INT NULL,
    [ReviewerID] INT NULL,
    [RegistrarID] INT NULL,
    [InstitutionalProcess] INT NULL,
    [ManagementLevel] INT NULL,
    [SwornDeclaration] BIT DEFAULT ((0)) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_PersonnelActionStatusHistory]') IS NULL
CREATE TABLE [HR].[tbl_PersonnelActionStatusHistory] (
    [HistoryId] INT IDENTITY(1,1) NOT NULL,
    [ActionId] INT NOT NULL,
    [FromStatus] NVARCHAR(30) NULL,
    [ToStatus] NVARCHAR(30) NOT NULL,
    [ChangedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [ChangedBy] INT NULL,
    [Notes] NVARCHAR(500) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_PersonnelMovements]') IS NULL
CREATE TABLE [HR].[tbl_PersonnelMovements] (
    [MovementID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    -- Nullable 2026-07-01: los movimientos originados por Acción de Personal
    -- (Traslado, Encargo) no tienen contrato asociado.
    [ContractID] INT NULL,
    [JobID] INT NOT NULL,
    [OriginDepartmentID] INT NULL,
    [DestinationDepartmentID] INT NOT NULL,
    [MovementDate] DATE NULL,
    [MovementType] NVARCHAR(30) NULL,
    [DocumentLocation] NVARCHAR(255) NULL,
    [Reason] NVARCHAR(500) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [PersonnelActionID] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Provinces]') IS NULL
CREATE TABLE [HR].[tbl_Provinces] (
    [ProvinceID] NVARCHAR(10) NOT NULL,
    [CountryID] NVARCHAR(10) NOT NULL,
    [ProvinceName] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Publications]') IS NULL
CREATE TABLE [HR].[tbl_Publications] (
    [PublicationID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [Location] NVARCHAR(100) NULL,
    [PublicationTypeID] INT NULL,
    [IsIndexed] BIT NULL,
    [JournalTypeID] INT NULL,
    [ISSN_ISBN] NVARCHAR(20) NULL,
    [JournalName] NVARCHAR(200) NULL,
    [JournalNumber] NVARCHAR(50) NULL,
    [Volume] NVARCHAR(50) NULL,
    [Pages] NVARCHAR(20) NULL,
    [KnowledgeAreaTypeID] INT NULL,
    [SubAreaTypeID] INT NULL,
    [AreaTypeID] INT NULL,
    [Title] NVARCHAR(300) NOT NULL,
    [OrganizedBy] NVARCHAR(150) NULL,
    [EventName] NVARCHAR(200) NULL,
    [EventEdition] NVARCHAR(50) NULL,
    [PublicationDate] DATE NULL,
    [UTAffiliation] BIT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_PunchJustifications]') IS NULL
CREATE TABLE [HR].[tbl_PunchJustifications] (
    [PunchJustID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [BossEmployeeID] INT NOT NULL,
    [JustificationTypeID] INT NOT NULL,
    [StartDate] DATETIME2 NULL,
    [EndDate] DATETIME2 NULL,
    [JustificationDate] DATETIME2 NULL,
    [Reason] NVARCHAR(500) NOT NULL,
    [HoursRequested] DECIMAL(4,2) NULL,
    [Approved] BIT DEFAULT ((0)) NOT NULL,
    [ApprovedAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NOT NULL,
    [Comments] NVARCHAR(1000) NULL,
    [Status] NVARCHAR(20) DEFAULT ('PENDING') NOT NULL,
    [PunchTypeID] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_ReportAudit]') IS NULL
CREATE TABLE [HR].[tbl_ReportAudit] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [UserEmail] NVARCHAR(255) NOT NULL,
    [ReportType] NVARCHAR(50) NOT NULL,
    [ReportFormat] NVARCHAR(10) NOT NULL,
    [FiltersApplied] NVARCHAR(MAX) NULL,
    [GeneratedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [FileSizeBytes] BIGINT NULL,
    [GenerationTimeMs] INT NULL,
    [ClientIp] NVARCHAR(50) NULL,
    [Success] BIT DEFAULT ((1)) NOT NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [FileName] NVARCHAR(255) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_RotationPatternDetails]') IS NULL
CREATE TABLE [HR].[tbl_RotationPatternDetails] (
    [PatternDetailID] INT IDENTITY(1,1) NOT NULL,
    [PatternID] INT NOT NULL,
    [DayOrder] INT NOT NULL,
    [ScheduleID] INT NULL,
    [IsRestDay] BIT DEFAULT ((0)) NOT NULL,
    [Notes] NVARCHAR(300) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_RotationPatterns]') IS NULL
CREATE TABLE [HR].[tbl_RotationPatterns] (
    [PatternID] INT IDENTITY(1,1) NOT NULL,
    [PatternCode] NVARCHAR(30) NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [PatternTypeID] INT NOT NULL,
    [CycleDays] INT NOT NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_SalaryHistory]') IS NULL
CREATE TABLE [HR].[tbl_SalaryHistory] (
    [SalaryHistoryID] INT IDENTITY(1,1) NOT NULL,
    [ContractID] INT NOT NULL,
    [OldSalary] DECIMAL(12,2) NOT NULL,
    [NewSalary] DECIMAL(12,2) NOT NULL,
    [ChangedBy] SYSNAME DEFAULT (suser_sname()) NOT NULL,
    [ChangedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [Reason] NVARCHAR(300) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_ScheduleChangePlan]') IS NULL
CREATE TABLE [HR].[tbl_ScheduleChangePlan] (
    [PlanID] INT IDENTITY(1,1) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Justification] NVARCHAR(1000) NULL,
    [RequestedByBossID] INT NOT NULL,
    [NewScheduleID] INT NOT NULL,
    [EffectiveDate] DATE NOT NULL,
    [ApplyAfterHours] TINYINT NULL,
    [EffectiveApplyDate] DATETIME2 NULL,
    [IsPermanent] BIT DEFAULT ((1)) NOT NULL,
    [TemporalEndDate] DATE NULL,
    [StatusTypeID] INT NOT NULL,
    [ApprovedByID] INT NULL,
    [ApprovedAt] DATETIME2 NULL,
    [RejectionReason] NVARCHAR(500) NULL,
    [AppliedAt] DATETIME2 NULL,
    [AppliedByID] INT NULL,
    [CreatedBy] INT NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [PlanCode] NVARCHAR(20) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_ScheduleChangePlanDetail]') IS NULL
CREATE TABLE [HR].[tbl_ScheduleChangePlanDetail] (
    [DetailID] INT IDENTITY(1,1) NOT NULL,
    [PlanID] INT NOT NULL,
    [EmployeeID] INT NOT NULL,
    [PreviousScheduleID] INT NULL,
    [PreviousEmpScheduleID] INT NULL,
    [AppliedEmpScheduleID] INT NULL,
    [StatusTypeID] INT NOT NULL,
    [Notes] NVARCHAR(500) NULL,
    [OmissionReason] NVARCHAR(300) NULL,
    [AppliedAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Schedules]') IS NULL
CREATE TABLE [HR].[tbl_Schedules] (
    [ScheduleID] INT IDENTITY(1,1) NOT NULL,
    [Description] NVARCHAR(120) NOT NULL,
    [EntryTime] TIME NOT NULL,
    [ExitTime] TIME NOT NULL,
    [WorkingDays] NVARCHAR(20) NOT NULL,
    [RequiredHoursPerDay] DECIMAL(5,2) NOT NULL,
    [HasLunchBreak] BIT DEFAULT ((1)) NOT NULL,
    [LunchStart] TIME NULL,
    [LunchEnd] TIME NULL,
    [IsRotating] BIT DEFAULT ((0)) NOT NULL,
    [RotationPattern] NVARCHAR(120) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL,
    [IsActive] BIT DEFAULT ((1)) NULL,
    [ScheduleCode] NVARCHAR(20) NULL,
    [CrossesMidnight] BIT DEFAULT ((0)) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[TBL_StoredFile]') IS NULL
CREATE TABLE [HR].[TBL_StoredFile] (
    [FileId] INT IDENTITY(1,1) NOT NULL,
    [FileGuid] UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [DirectoryCode] NVARCHAR(50) NOT NULL,
    [EntityType] NVARCHAR(50) NOT NULL,
    [EntityId] NVARCHAR(100) NOT NULL,
    [UploadYear] SMALLINT NOT NULL,
    [RelativeFolder] NVARCHAR(600) NOT NULL,
    [StoredFileName] NVARCHAR(260) NOT NULL,
    [OriginalFileName] NVARCHAR(260) NULL,
    [Extension] NVARCHAR(20) NULL,
    [ContentType] NVARCHAR(100) NULL,
    [SizeBytes] BIGINT NOT NULL,
    [Sha256] BINARY(32) NULL,
    [Status] TINYINT DEFAULT ((1)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL,
    [DeletedAt] DATETIME2 NULL,
    [DeletedBy] INT NULL,
    [FilePathHash] BINARY(32) NULL,
    [DocumentTypeId] INT NULL,
    -- Número/fecha de la resolución u oficio cuando DocumentTypeId corresponde a un
    -- documento referenciable en plantillas de contratos (ej. RESOLUCION_CAU, MEMORANDO_RECTORADO).
    [DocumentReferenceNumber] NVARCHAR(100) NULL,
    [DocumentReferenceDate] DATE NULL
);
GO

IF COL_LENGTH('[HR].[TBL_StoredFile]', 'DocumentReferenceNumber') IS NULL
    ALTER TABLE [HR].[TBL_StoredFile] ADD [DocumentReferenceNumber] NVARCHAR(100) NULL;
GO

IF COL_LENGTH('[HR].[TBL_StoredFile]', 'DocumentReferenceDate') IS NULL
    ALTER TABLE [HR].[TBL_StoredFile] ADD [DocumentReferenceDate] DATE NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Subrogations]') IS NULL
CREATE TABLE [HR].[tbl_Subrogations] (
    [SubrogationID] INT IDENTITY(1,1) NOT NULL,
    [SubrogatedEmployeeID] INT NOT NULL,
    [SubrogatingEmployeeID] INT NOT NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NOT NULL,
    [PermissionID] INT NULL,
    [VacationID] INT NULL,
    [Reason] NVARCHAR(300) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_TeacherStructure]') IS NULL
CREATE TABLE [HR].[tbl_TeacherStructure] (
    [TeacherStructureID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [DedicationTypeID] INT NOT NULL,
    [WeeklyClassHours] DECIMAL(5,2) NULL,
    [HourValue] DECIMAL(10,4) NULL,
    [RMU] DECIMAL(10,2) NULL,
    [DepartmentID] INT NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [EligiblePromotion] BIT DEFAULT ((0)) NOT NULL,
    [EligibleRecategory] BIT DEFAULT ((0)) NOT NULL,
    [EligibleDedicChg] BIT DEFAULT ((0)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL
    [UpdatedBy] INT NULL
    [LadderID] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_TimeBalanceMovements]') IS NULL
CREATE TABLE [HR].[tbl_TimeBalanceMovements] (
    [MovementID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [DeltaVacationMin] INT DEFAULT ((0)) NOT NULL,
    [DeltaRecoveryMin] INT DEFAULT ((0)) NOT NULL,
    [MovementAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [SourceModule] NVARCHAR(50) NULL,
    [SourceTable] NVARCHAR(128) NULL,
    [SourceID] NVARCHAR(128) NULL,
    [PerformedByEmpID] INT NULL,
    [Note] NVARCHAR(2000) NULL
);
GO

-- 2026-07-06 (Fase 3, propuesta multi-régimen): régimen que originó el
-- movimiento. Nullable por historial previo a este cambio.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_TimeBalanceMovements]') AND name = 'LaborRegimeId'
)
    ALTER TABLE [HR].[tbl_TimeBalanceMovements] ADD [LaborRegimeId] INT NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_TimeBalances]') IS NULL
CREATE TABLE [HR].[tbl_TimeBalances] (
    [EmployeeID] INT NOT NULL,
    [VacationAvailableMin] INT DEFAULT ((0)) NOT NULL,
    [RecoveryPendingMin] INT DEFAULT ((0)) NOT NULL,
    [LastUpdated] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- 2026-07-06 (Fase 3, propuesta multi-régimen): saldo separado por régimen
-- laboral en vez de uno solo por empleado. Backfill ya ejecutado en
-- producción: filas existentes -> régimen IsPrincipal (o el único activo, o
-- EmployeeType como espejo para empleados inactivos sin fila en
-- tbl_EmployeeLaborRegime); régimen secundario de empleados multi-régimen
-- arranca en 0 (no se inventa historia). La PK pasó de (EmployeeID) a
-- (EmployeeID, LaborRegimeId) — ver 02_constraints.sql.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_TimeBalances]') AND name = 'LaborRegimeId'
)
    ALTER TABLE [HR].[tbl_TimeBalances] ADD [LaborRegimeId] INT NOT NULL DEFAULT (57);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_TimePlanning]') IS NULL
CREATE TABLE [HR].[tbl_TimePlanning] (
    [PlanID] INT IDENTITY(1,1) NOT NULL,
    [PlanType] NVARCHAR(20) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NOT NULL,
    [StartTime] TIME NOT NULL,
    [EndTime] TIME NOT NULL,
    [OvertimeType] NVARCHAR(50) NULL,
    [Factor] DECIMAL(5,2) NULL,
    [OwedMinutes] INT NULL,
    [PlanStatusTypeID] INT NOT NULL,
    [RequiresApproval] BIT DEFAULT ((1)) NOT NULL,
    [ApprovedBy] INT NULL,
    [SecondApprover] INT NULL,
    [ApprovedAt] DATETIME2 NULL,
    [CreatedBy] INT NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_TimePlanningEmployees]') IS NULL
CREATE TABLE [HR].[tbl_TimePlanningEmployees] (
    [PlanEmployeeID] INT IDENTITY(1,1) NOT NULL,
    [PlanID] INT NOT NULL,
    [EmployeeID] INT NOT NULL,
    [AssignedHours] DECIMAL(5,2) NULL,
    [AssignedMinutes] INT NULL,
    [ActualHours] DECIMAL(5,2) DEFAULT ((0)) NULL,
    [ActualMinutes] INT DEFAULT ((0)) NULL,
    [EmployeeStatusTypeID] INT NOT NULL,
    [PaymentAmount] DECIMAL(12,2) DEFAULT ((0)) NULL,
    [IsEligible] BIT DEFAULT ((1)) NOT NULL,
    [EligibilityReason] NVARCHAR(300) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_TimePlanningExecution]') IS NULL
CREATE TABLE [HR].[tbl_TimePlanningExecution] (
    [ExecutionID] INT IDENTITY(1,1) NOT NULL,
    [PlanEmployeeID] INT NOT NULL,
    [WorkDate] DATE NOT NULL,
    [StartTime] DATETIME2 NULL,
    [EndTime] DATETIME2 NULL,
    [TotalMinutes] INT DEFAULT ((0)) NOT NULL,
    [RegularMinutes] INT DEFAULT ((0)) NOT NULL,
    [OvertimeMinutes] INT DEFAULT ((0)) NOT NULL,
    [NightMinutes] INT DEFAULT ((0)) NOT NULL,
    [HolidayMinutes] INT DEFAULT ((0)) NOT NULL,
    [VerifiedBy] INT NULL,
    [VerifiedAt] DATETIME2 NULL,
    [Comments] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL
);
GO

-- Minutos trabajados fuera de la ventana planificada (antes del inicio o
-- después del fin del plan) — antes se descartaban en silencio.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_TimePlanningExecution]') AND name = 'ExceededMinutes'
)
    ALTER TABLE [HR].[tbl_TimePlanningExecution] ADD [ExceededMinutes] INT DEFAULT ((0)) NULL;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_TimeRecoveryLogs]') IS NULL
CREATE TABLE [HR].[tbl_TimeRecoveryLogs] (
    [RecoveryLogID] INT IDENTITY(1,1) NOT NULL,
    [RecoveryPlanID] INT NOT NULL,
    [ExecutedDate] DATE NOT NULL,
    [MinutesRecovered] INT NOT NULL,
    [ApprovedBy] INT NULL,
    [ApprovedAt] DATETIME2 NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_TimeRecoveryPlans]') IS NULL
CREATE TABLE [HR].[tbl_TimeRecoveryPlans] (
    [RecoveryPlanID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [OwedMinutes] INT NOT NULL,
    [PlanDate] DATE NOT NULL,
    [FromTime] TIME NOT NULL,
    [ToTime] TIME NOT NULL,
    [Reason] NVARCHAR(300) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [RowVersion] TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Trainings]') IS NULL
CREATE TABLE [HR].[tbl_Trainings] (
    [TrainingID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [Location] NVARCHAR(100) NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Institution] NVARCHAR(150) NOT NULL,
    [KnowledgeAreaTypeID] INT NULL,
    [EventTypeID] INT NULL,
    [CertifiedBy] NVARCHAR(150) NULL,
    [CertificateTypeID] INT NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NOT NULL,
    [Hours] INT NOT NULL,
    [ApprovalTypeID] INT NULL,
    -- Direccion (recibida/impartida), modalidad y pais: agregados para el
    -- modulo de promocion academica docente (academic-promotion). Catalogos
    -- ref_Types: TRAINING_DIRECTION, TRAINING_MODALITY. Pais: HR.tbl_Countries.
    [TrainingDirectionTypeID] INT NULL,
    [ModalityTypeID] INT NULL,
    [CountryID] NVARCHAR(10) NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Vacations]') IS NULL
CREATE TABLE [HR].[tbl_Vacations] (
    [VacationID] INT IDENTITY(1,1) NOT NULL,
    [EmployeeID] INT NOT NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NOT NULL,
    [DaysGranted] INT NOT NULL,
    [DaysTaken] INT DEFAULT ((0)) NOT NULL,
    [Status] NVARCHAR(20) DEFAULT ('Planned') NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RowVersion] TIMESTAMP NOT NULL,
    [ApprovedBy] INT NULL,
    [ApprovedAt] DATETIME2 NULL,
    [CreatedBy] INT NULL,
    [UpdatedBy] INT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_WorkExperiences]') IS NULL
CREATE TABLE [HR].[tbl_WorkExperiences] (
    [WorkExpID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [CountryID] NVARCHAR(10) NULL,
    [Company] NVARCHAR(150) NOT NULL,
    [InstitutionTypeID] INT NULL,
    [EntryReason] NVARCHAR(200) NULL,
    [ExitReason] NVARCHAR(200) NULL,
    [Position] NVARCHAR(120) NOT NULL,
    [InstitutionAddress] NVARCHAR(255) NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NULL,
    [ExperienceTypeID] INT NULL,
    [IsCurrent] BIT DEFAULT ((0)) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- ============================================================================
-- Tabla: HR.tbl_Languages
-- Propósito: certificaciones de idioma del empleado (hoja de vida / perfil
-- personal), igual dominio que WorkExperiences/Publications/Trainings.
-- Consumida tambien por el modulo de promocion academica docente.
-- Catalogos: LANGUAGE (idioma), LANGUAGE_LEVEL (A1..C2, marco CEFR).
-- ============================================================================
-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_Languages]') IS NULL
CREATE TABLE [HR].[tbl_Languages] (
    [LanguageID] INT IDENTITY(1,1) NOT NULL,
    [PersonID] INT NOT NULL,
    [LanguageTypeID] INT NOT NULL,
    [LevelTypeID] INT NOT NULL,
    [ReferenceFramework] NVARCHAR(50) DEFAULT ('CEFR') NULL,
    [CertifyingInstitution] NVARCHAR(150) NULL,
    [CountryID] NVARCHAR(10) NULL,
    [IssueDate] DATE NOT NULL,
    [ExpirationDate] DATE NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] INT NULL
);
GO

-- Catalogos ref_Types requeridos por tbl_Languages.
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'LANGUAGE' AND [Name] = 'ENGLISH')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('LANGUAGE', 'ENGLISH', 'Inglés', 1),
       ('LANGUAGE', 'FRENCH', 'Francés', 1),
       ('LANGUAGE', 'GERMAN', 'Alemán', 1),
       ('LANGUAGE', 'PORTUGUESE', 'Portugués', 1),
       ('LANGUAGE', 'ITALIAN', 'Italiano', 1),
       ('LANGUAGE', 'OTHER', 'Otro', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'LANGUAGE_LEVEL' AND [Name] = 'A1')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('LANGUAGE_LEVEL', 'A1', 'A1 - Principiante', 1),
       ('LANGUAGE_LEVEL', 'A2', 'A2 - Básico', 1),
       ('LANGUAGE_LEVEL', 'B1', 'B1 - Intermedio', 1),
       ('LANGUAGE_LEVEL', 'B2', 'B2 - Intermedio alto', 1),
       ('LANGUAGE_LEVEL', 'C1', 'C1 - Avanzado', 1),
       ('LANGUAGE_LEVEL', 'C2', 'C2 - Dominio', 1);
GO

-- Catalogos ref_Types requeridos por tbl_Trainings (direccion/modalidad, ver ALTER arriba).
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'TRAINING_DIRECTION' AND [Name] = 'RECEIVED_TRAINING')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('TRAINING_DIRECTION', 'RECEIVED_TRAINING', 'Capacitación recibida', 1),
       ('TRAINING_DIRECTION', 'GIVEN_TRAINING', 'Capacitación impartida', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'TRAINING_MODALITY' AND [Name] = 'ONLINE')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('TRAINING_MODALITY', 'ONLINE', 'En línea', 1),
       ('TRAINING_MODALITY', 'IN_PERSON', 'Presencial', 1),
       ('TRAINING_MODALITY', 'HYBRID', 'Híbrida', 1);
GO

-- Directorio de documentos de soporte para certificaciones de idioma
-- (mismo motor DocumentsController/StoredFile ya usado en el resto del sistema).
IF NOT EXISTS (SELECT 1 FROM [HR].[TBL_DirectoryParameters] WHERE [Code] = 'HR_LANGUAGE_CERTIFICATION')
INSERT INTO [HR].[TBL_DirectoryParameters] ([Code], [PhysicalPath], [RelativePath], [Description], [Extension], [MaxSizeMB], [Status])
VALUES ('HR_LANGUAGE_CERTIFICATION', '\\nas11.uta.edu.ec\ArchUTA1\HR\languages\', '\\nas11.uta.edu.ec\ArchUTA1\HR\languages\', 'Certificados de idioma del empleado', '.pdf', 10, 1);
GO

-- ============================================================================
-- Tabla: HR.tbl_UserAccessScopes
-- Propósito: define qué departamentos/facultades puede ver o gestionar un
-- usuario, por módulo/trámite (Contratos, Acciones de Personal, u otros
-- trámites futuros vía ref_Types), evitando que vea datos de toda la
-- institución. Genérico y extensible: agregar un trámite nuevo solo
-- requiere una fila más en ACCESS_MODULE_TYPE, sin tocar el esquema.
-- ============================================================================

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_UserAccessScopes]') IS NULL
CREATE TABLE [HR].[tbl_UserAccessScopes] (
    [Id]               INT IDENTITY(1,1) NOT NULL,

    -- Empleado (HR.tbl_Employees) al que se le otorga el acceso.
    -- En runtime se cruza contra ICurrentUserService.EmployeeId (claim del JWT).
    [EmployeeId]       INT NOT NULL,

    -- Módulo al que aplica esta asignación.
    -- FK -> HR.ref_Types con Category = 'ACCESS_MODULE_TYPE'
    -- Valores: CONTRACTS, PERSONNEL_ACTIONS (extensible)
    [ModuleTypeId]     INT NOT NULL,

    -- Tipo de alcance del acceso.
    -- FK -> HR.ref_Types con Category = 'ACCESS_SCOPE_TYPE'
    -- Valores: GLOBAL (ve todo, ignora DepartmentId),
    --          DEPARTMENT_TREE (ese departamento + todos sus hijos, ej. una Facultad completa),
    --          DEPARTMENT_ONLY (solo ese departamento exacto, sin hijos)
    [ScopeTypeId]      INT NOT NULL,

    -- Departamento/Facultad asignado. NULL únicamente cuando ScopeTypeId = GLOBAL.
    -- FK -> HR.tbl_Departments
    [DepartmentId]     INT NULL,

    [IsActive]         BIT DEFAULT ((1)) NOT NULL,
    -- Vigencia de la asignación. AssignedAt = inicio. ExpiresAt NULL = sin fecha de fin.
    [AssignedAt]       DATETIME2 DEFAULT (sysutcdatetime()) NOT NULL,
    [ExpiresAt]        DATETIME2 NULL,
    [AssignedBy]       NVARCHAR(320) NULL,
    [Reason]           NVARCHAR(300) NULL,

    [CreatedAt]        DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy]        INT NULL,
    [UpdatedAt]        DATETIME2 NULL,
    [UpdatedBy]        INT NULL,
    [RowVersion]       TIMESTAMP NOT NULL
);
GO

-- ============================================================================
-- Tabla: HR.tbl_UserAccessScopeHistory
-- Propósito: historial inmutable de cada cambio (asignación, modificación,
-- remoción) sobre tbl_UserAccessScopes. Independiente de la fila "viva",
-- así nunca se pierde el rastro aunque esa fila se reactive o desactive.
-- ============================================================================

-- ============================================================================
-- Tabla: HR.tbl_EmployeeLaborRegime
-- Propósito: régimen(es) laboral(es) (LOSEP/LOES/CT) vigentes o históricos de
-- un empleado. Un empleado puede tener varias filas activas simultáneas (ej.
-- nombramiento LOSEP en Dirección Administrativa + contrato LOES ocasional
-- como docente en otra facultad). Reemplaza a HR.tbl_Employees.EmployeeType
-- como fuente de verdad; ese campo se mantiene como espejo del régimen
-- IsPrincipal para no romper a los consumidores existentes.
-- ============================================================================

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_EmployeeLaborRegime]') IS NULL
CREATE TABLE [HR].[tbl_EmployeeLaborRegime] (
    [Id]                       INT IDENTITY(1,1) NOT NULL,
    [EmployeeId]               INT NOT NULL,

    -- FK -> HR.ref_Types (Category='CONTRACT_TYPE'). 57=LOSEP, 58=LOES, 59=Código Trabajo.
    [LaborRegimeId]            INT NOT NULL,

    -- Departamento/cargo donde se ejerce ESTE régimen (no el del empleado en general).
    [DepartmentId]             INT NULL,
    [JobId]                    INT NULL,

    -- true = nombramiento (fijo/provisional, sin vencimiento); false = régimen temporal.
    [IsIndefinite]             BIT DEFAULT ((0)) NOT NULL,

    -- 'CONTRACT' | 'PERSONNEL_ACTION'
    [DocumentType]             NVARCHAR(20) NOT NULL,
    [DocumentNumber]           NVARCHAR(50) NULL,
    [SourceContractId]         INT NULL,
    [SourcePersonnelActionId]  INT NULL,

    [EffectiveFrom]            DATE NOT NULL,
    [EffectiveTo]              DATE NULL,
    [IsActive]                 BIT DEFAULT ((1)) NOT NULL,

    -- Calculado por la aplicación: nombramiento gana; si ninguno, gana LOSEP.
    [IsPrincipal]              BIT DEFAULT ((0)) NOT NULL,

    [CreatedAt]                DATETIME2 DEFAULT (getdate()) NOT NULL,
    [CreatedBy]                INT NULL,
    [UpdatedAt]                DATETIME2 NULL,
    [UpdatedBy]                INT NULL,
    [RowVersion]               TIMESTAMP NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[HR].[tbl_UserAccessScopeHistory]') IS NULL
CREATE TABLE [HR].[tbl_UserAccessScopeHistory] (
    [Id]               BIGINT IDENTITY(1,1) NOT NULL,

    -- Referencia informativa a la fila viva (no se borra aunque la fila ya no exista).
    [ScopeId]          INT NULL,

    [EmployeeId]       INT NOT NULL,
    [ModuleTypeId]     INT NOT NULL,

    -- 'Assigned' | 'Modified' | 'Removed'
    [ChangeType]       NVARCHAR(20) NOT NULL,

    -- Snapshot de los valores ANTES y DESPUÉS del cambio (para auditoría completa).
    [PreviousScopeTypeId]  INT NULL,
    [PreviousDepartmentId] INT NULL,
    [NewScopeTypeId]       INT NULL,
    [NewDepartmentId]      INT NULL,

    [ChangedBy]        NVARCHAR(320) NOT NULL,
    [ChangeReason]     NVARCHAR(300) NULL,
    [ChangeDateTime]   DATETIME2 DEFAULT (sysutcdatetime()) NOT NULL
);
GO