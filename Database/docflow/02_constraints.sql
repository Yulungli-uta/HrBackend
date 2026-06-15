-- ============================================================
-- CONSTRAINTS (PK + UNIQUE + FK): esquema [docflow]
-- Orden garantizado: PKs primero, FKs en orden topológico de dependencias
-- Generado: 2026-05-29
-- ============================================================

SET NOCOUNT ON;
GO

-- ============================================================
-- BLOQUE 1: PRIMARY KEYS
-- (Deben existir antes de crear cualquier FK que las referencie)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_DocumentRules')
    ALTER TABLE [docflow].[tbl_DocumentRules]
        ADD CONSTRAINT [PK_DocumentRules] PRIMARY KEY CLUSTERED ([RuleId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Documents')
    ALTER TABLE [docflow].[tbl_Documents]
        ADD CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED ([DocumentId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_FileVersions')
    ALTER TABLE [docflow].[tbl_FileVersions]
        ADD CONSTRAINT [PK_FileVersions] PRIMARY KEY CLUSTERED ([VersionId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ProcessHierarchy')
    ALTER TABLE [docflow].[tbl_ProcessHierarchy]
        ADD CONSTRAINT [PK_ProcessHierarchy] PRIMARY KEY CLUSTERED ([ProcessId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ProcessTransitions')
    ALTER TABLE [docflow].[tbl_ProcessTransitions]
        ADD CONSTRAINT [PK_ProcessTransitions] PRIMARY KEY CLUSTERED ([TransitionId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_WorkflowInstances')
    ALTER TABLE [docflow].[tbl_WorkflowInstances]
        ADD CONSTRAINT [PK_WorkflowInstances] PRIMARY KEY CLUSTERED ([InstanceId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_WorkflowMovements')
    ALTER TABLE [docflow].[tbl_WorkflowMovements]
        ADD CONSTRAINT [PK_WorkflowMovements] PRIMARY KEY CLUSTERED ([MovementId]);
GO

-- ============================================================
-- BLOQUE 2: UNIQUE CONSTRAINTS
-- ============================================================

-- ============================================================
-- BLOQUE 3: FOREIGN KEYS
-- (Ordenadas topológicamente: tablas independientes primero)
-- ============================================================

-- --- Tabla: tbl_WorkflowMovements ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Movements_Instance')
    ALTER TABLE [docflow].[tbl_WorkflowMovements]
        ADD CONSTRAINT [FK_Movements_Instance]
            FOREIGN KEY ([InstanceId])
            REFERENCES [docflow].[tbl_WorkflowInstances] ([InstanceId]);
GO

-- --- Tabla: tbl_FileVersions ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileVersions_Document')
    ALTER TABLE [docflow].[tbl_FileVersions]
        ADD CONSTRAINT [FK_FileVersions_Document]
            FOREIGN KEY ([DocumentId])
            REFERENCES [docflow].[tbl_Documents] ([DocumentId]);
GO

-- --- Tabla: tbl_ProcessHierarchy ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProcessHierarchy_Parent')
    ALTER TABLE [docflow].[tbl_ProcessHierarchy]
        ADD CONSTRAINT [FK_ProcessHierarchy_Parent]
            FOREIGN KEY ([ParentId])
            REFERENCES [docflow].[tbl_ProcessHierarchy] ([ProcessId]);
GO

-- --- Tabla: tbl_DocumentRules ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocRules_Process')
    ALTER TABLE [docflow].[tbl_DocumentRules]
        ADD CONSTRAINT [FK_DocRules_Process]
            FOREIGN KEY ([ProcessId])
            REFERENCES [docflow].[tbl_ProcessHierarchy] ([ProcessId]);
GO

-- --- Tabla: tbl_ProcessTransitions ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Transitions_ReturnToProcess')
    ALTER TABLE [docflow].[tbl_ProcessTransitions]
        ADD CONSTRAINT [FK_Transitions_ReturnToProcess]
            FOREIGN KEY ([ReturnToProcessId])
            REFERENCES [docflow].[tbl_ProcessHierarchy] ([ProcessId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Transitions_ToProcess')
    ALTER TABLE [docflow].[tbl_ProcessTransitions]
        ADD CONSTRAINT [FK_Transitions_ToProcess]
            FOREIGN KEY ([ToProcessId])
            REFERENCES [docflow].[tbl_ProcessHierarchy] ([ProcessId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Transitions_FromProcess')
    ALTER TABLE [docflow].[tbl_ProcessTransitions]
        ADD CONSTRAINT [FK_Transitions_FromProcess]
            FOREIGN KEY ([FromProcessId])
            REFERENCES [docflow].[tbl_ProcessHierarchy] ([ProcessId]);
GO

-- --- Tabla: tbl_Documents ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Documents_Rule')
    ALTER TABLE [docflow].[tbl_Documents]
        ADD CONSTRAINT [FK_Documents_Rule]
            FOREIGN KEY ([RuleId])
            REFERENCES [docflow].[tbl_DocumentRules] ([RuleId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Documents_Instance')
    ALTER TABLE [docflow].[tbl_Documents]
        ADD CONSTRAINT [FK_Documents_Instance]
            FOREIGN KEY ([InstanceId])
            REFERENCES [docflow].[tbl_WorkflowInstances] ([InstanceId]);
GO

-- --- Tabla: tbl_WorkflowInstances ---
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Instances_Process')
    ALTER TABLE [docflow].[tbl_WorkflowInstances]
        ADD CONSTRAINT [FK_Instances_Process]
            FOREIGN KEY ([ProcessId])
            REFERENCES [docflow].[tbl_ProcessHierarchy] ([ProcessId]);
GO
