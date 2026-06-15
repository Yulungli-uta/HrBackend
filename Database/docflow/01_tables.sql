-- ============================================================
-- TABLAS: esquema [docflow]
-- Generado: 2026-05-29
-- ============================================================

SET NOCOUNT ON;
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[docflow].[tbl_DocumentRules]') IS NULL
CREATE TABLE [docflow].[tbl_DocumentRules] (
    [RuleId] INT IDENTITY(1,1) NOT NULL,
    [ProcessId] INT NOT NULL,
    [DocumentType] NVARCHAR(100) NOT NULL,
    [IsRequired] BIT DEFAULT ((1)) NOT NULL,
    [DefaultVisibility] TINYINT DEFAULT ((1)) NOT NULL,
    [AllowVisibilityOverride] BIT DEFAULT ((0)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[docflow].[tbl_Documents]') IS NULL
CREATE TABLE [docflow].[tbl_Documents] (
    [DocumentId] UNIQUEIDENTIFIER NOT NULL,
    [InstanceId] UNIQUEIDENTIFIER NOT NULL,
    [RuleId] INT NULL,
    [DocumentName] NVARCHAR(255) NOT NULL,
    [CreatedByDepartmentId] INT NOT NULL,
    [Visibility] TINYINT DEFAULT ((1)) NOT NULL,
    [CurrentVersion] INT DEFAULT ((0)) NOT NULL,
    [IsDeleted] BIT DEFAULT ((0)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[docflow].[tbl_FileVersions]') IS NULL
CREATE TABLE [docflow].[tbl_FileVersions] (
    [VersionId] UNIQUEIDENTIFIER NOT NULL,
    [DocumentId] UNIQUEIDENTIFIER NOT NULL,
    [VersionNumber] INT NOT NULL,
    [StoragePath] NVARCHAR(1000) NOT NULL,
    [FileExtension] NVARCHAR(20) NULL,
    [FileSizeInBytes] BIGINT NULL,
    [ChecksumHash] NVARCHAR(128) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[docflow].[tbl_ProcessHierarchy]') IS NULL
CREATE TABLE [docflow].[tbl_ProcessHierarchy] (
    [ProcessId] INT IDENTITY(1,1) NOT NULL,
    [ParentId] INT NULL,
    [ProcessCode] NVARCHAR(50) NOT NULL,
    [ProcessName] NVARCHAR(200) NOT NULL,
    [ResponsibleDepartmentId] INT NOT NULL,
    [ProcessFolderName] NVARCHAR(100) NULL,
    [DynamicFieldMetadata] NVARCHAR(MAX) NULL,
    [IsActive] BIT DEFAULT ((1)) NOT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[docflow].[tbl_ProcessTransitions]') IS NULL
CREATE TABLE [docflow].[tbl_ProcessTransitions] (
    [TransitionId] INT IDENTITY(1,1) NOT NULL,
    [FromProcessId] INT NOT NULL,
    [ToProcessId] INT NOT NULL,
    [IsDefault] BIT DEFAULT ((1)) NOT NULL,
    [AllowReturn] BIT DEFAULT ((1)) NOT NULL,
    [ReturnToProcessId] INT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[docflow].[tbl_WorkflowInstances]') IS NULL
CREATE TABLE [docflow].[tbl_WorkflowInstances] (
    [InstanceId] UNIQUEIDENTIFIER NOT NULL,
    [ProcessId] INT NOT NULL,
    [CurrentStatus] NVARCHAR(50) NOT NULL,
    [CurrentDepartmentId] INT NOT NULL,
    [AssignedToUserId] INT NULL,
    [DynamicMetadata] NVARCHAR(MAX) NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL,
    [UpdatedBy] INT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [RootProcessId] INT NULL,
    [InstanceName] NVARCHAR(255) NULL
);
GO

-- ------------------------------------------------------------
IF OBJECT_ID('[docflow].[tbl_WorkflowMovements]') IS NULL
CREATE TABLE [docflow].[tbl_WorkflowMovements] (
    [MovementId] UNIQUEIDENTIFIER NOT NULL,
    [InstanceId] UNIQUEIDENTIFIER NOT NULL,
    [MovementType] NVARCHAR(10) NOT NULL,
    [Comments] NVARCHAR(2000) NULL,
    [AssignedToUserId] INT NULL,
    [FromProcessId] INT NULL,
    [ToProcessId] INT NULL,
    [FromDepartmentId] INT NULL,
    [ToDepartmentId] INT NULL,
    [CreatedBy] INT NULL,
    [CreatedAt] DATETIME2 DEFAULT (getdate()) NOT NULL
);
GO