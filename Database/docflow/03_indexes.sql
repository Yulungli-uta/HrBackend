-- ============================================================
-- ÍNDICES NONCLUSTERED: esquema [docflow]
-- ============================================================

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_DocRules_Process')
    CREATE NONCLUSTERED INDEX [IX_DocRules_Process] ON [docflow].[tbl_DocumentRules] ([ProcessId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_DocRules_Process_Required')
    CREATE NONCLUSTERED INDEX [IX_DocRules_Process_Required] ON [docflow].[tbl_DocumentRules] ([ProcessId], [IsRequired]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Documents_Instance_Visibility_Dept')
    CREATE NONCLUSTERED INDEX [IX_Documents_Instance_Visibility_Dept] ON [docflow].[tbl_Documents] ([InstanceId], [Visibility], [CreatedByDepartmentId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Documents_Rule')
    CREATE NONCLUSTERED INDEX [IX_Documents_Rule] ON [docflow].[tbl_Documents] ([RuleId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_FileVersions_Document_Version')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_FileVersions_Document_Version] ON [docflow].[tbl_FileVersions] ([DocumentId], [VersionNumber]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Process_Parent')
    CREATE NONCLUSTERED INDEX [IX_Process_Parent] ON [docflow].[tbl_ProcessHierarchy] ([ParentId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Process_ResponsibleDept')
    CREATE NONCLUSTERED INDEX [IX_Process_ResponsibleDept] ON [docflow].[tbl_ProcessHierarchy] ([ResponsibleDepartmentId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Process_Code')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Process_Code] ON [docflow].[tbl_ProcessHierarchy] ([ProcessCode]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Transitions_From_Default')
    CREATE NONCLUSTERED INDEX [IX_Transitions_From_Default] ON [docflow].[tbl_ProcessTransitions] ([FromProcessId], [IsDefault]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Transitions_From_To')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Transitions_From_To] ON [docflow].[tbl_ProcessTransitions] ([FromProcessId], [ToProcessId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Instances_CurrentDept_Status')
    CREATE NONCLUSTERED INDEX [IX_Instances_CurrentDept_Status] ON [docflow].[tbl_WorkflowInstances] ([CurrentDepartmentId], [CurrentStatus], [CreatedAt] DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Instances_Process')
    CREATE NONCLUSTERED INDEX [IX_Instances_Process] ON [docflow].[tbl_WorkflowInstances] ([ProcessId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Movements_FromToDept')
    CREATE NONCLUSTERED INDEX [IX_Movements_FromToDept] ON [docflow].[tbl_WorkflowMovements] ([FromDepartmentId], [ToDepartmentId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Movements_FromToProcess')
    CREATE NONCLUSTERED INDEX [IX_Movements_FromToProcess] ON [docflow].[tbl_WorkflowMovements] ([FromProcessId], [ToProcessId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Movements_Instance_CreatedAt')
    CREATE NONCLUSTERED INDEX [IX_Movements_Instance_CreatedAt] ON [docflow].[tbl_WorkflowMovements] ([InstanceId], [CreatedAt] DESC);
GO
