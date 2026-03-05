/* ============================================================
   DOCFLOW - CREACIÓN DESDE CERO (MEJORADO)
   BLOQUE 2/3: CONSTRAINTS + DEFAULTS + CHECKS + ÍNDICES
   - PK
   - FK
   - DEFAULT constraints
   - CHECK constraints
   - INDEX / UNIQUE INDEX
   ============================================================ */

-- =========================
-- DEFAULT CONSTRAINTS
-- =========================

-- ALTER TABLE docflow.tbl_Users
-- ADD CONSTRAINT DF_Users_IsActive DEFAULT(1) FOR IsActive;

-- ALTER TABLE docflow.tbl_Users
-- ADD CONSTRAINT DF_Users_CreatedAt DEFAULT(GETDATE()) FOR CreatedAt;

ALTER TABLE docflow.tbl_ProcessHierarchy
ADD CONSTRAINT DF_Process_IsActive DEFAULT(1) FOR IsActive;

ALTER TABLE docflow.tbl_ProcessHierarchy
ADD CONSTRAINT DF_Process_CreatedAt DEFAULT(GETDATE()) FOR CreatedAt;

ALTER TABLE docflow.tbl_ProcessTransitions
ADD CONSTRAINT DF_Transitions_IsDefault DEFAULT(1) FOR IsDefault;

ALTER TABLE docflow.tbl_ProcessTransitions
ADD CONSTRAINT DF_Transitions_AllowReturn DEFAULT(1) FOR AllowReturn;

ALTER TABLE docflow.tbl_ProcessTransitions
ADD CONSTRAINT DF_Transitions_CreatedAt DEFAULT(GETDATE()) FOR CreatedAt;

ALTER TABLE docflow.tbl_DocumentRules
ADD CONSTRAINT DF_DocRules_IsRequired DEFAULT(1) FOR IsRequired;

ALTER TABLE docflow.tbl_DocumentRules
ADD CONSTRAINT DF_DocRules_DefaultVisibility DEFAULT(1) FOR DefaultVisibility;

ALTER TABLE docflow.tbl_DocumentRules
ADD CONSTRAINT DF_DocRules_AllowVisibilityOverride DEFAULT(0) FOR AllowVisibilityOverride;

ALTER TABLE docflow.tbl_DocumentRules
ADD CONSTRAINT DF_DocRules_CreatedAt DEFAULT(GETDATE()) FOR CreatedAt;

ALTER TABLE docflow.tbl_WorkflowInstances
ADD CONSTRAINT DF_Instances_CreatedAt DEFAULT(GETDATE()) FOR CreatedAt;

ALTER TABLE docflow.tbl_Documents
ADD CONSTRAINT DF_Documents_Visibility DEFAULT(1) FOR Visibility;

ALTER TABLE docflow.tbl_Documents
ADD CONSTRAINT DF_Documents_CurrentVersion DEFAULT(0) FOR CurrentVersion;

ALTER TABLE docflow.tbl_Documents
ADD CONSTRAINT DF_Documents_IsDeleted DEFAULT(0) FOR IsDeleted;

ALTER TABLE docflow.tbl_Documents
ADD CONSTRAINT DF_Documents_CreatedAt DEFAULT(GETDATE()) FOR CreatedAt;

ALTER TABLE docflow.tbl_FileVersions
ADD CONSTRAINT DF_FileVersions_CreatedAt DEFAULT(GETDATE()) FOR CreatedAt;

ALTER TABLE docflow.tbl_WorkflowMovements
ADD CONSTRAINT DF_Movements_CreatedAt DEFAULT(GETDATE()) FOR CreatedAt;

-- =========================
-- PRIMARY KEYS
-- =========================

-- ALTER TABLE docflow.tbl_Users
-- ADD CONSTRAINT PK_Users PRIMARY KEY (UserId);

ALTER TABLE docflow.tbl_ProcessHierarchy
ADD CONSTRAINT PK_ProcessHierarchy PRIMARY KEY (ProcessId);

ALTER TABLE docflow.tbl_ProcessTransitions
ADD CONSTRAINT PK_ProcessTransitions PRIMARY KEY (TransitionId);

ALTER TABLE docflow.tbl_DocumentRules
ADD CONSTRAINT PK_DocumentRules PRIMARY KEY (RuleId);

ALTER TABLE docflow.tbl_WorkflowInstances
ADD CONSTRAINT PK_WorkflowInstances PRIMARY KEY (InstanceId);

ALTER TABLE docflow.tbl_Documents
ADD CONSTRAINT PK_Documents PRIMARY KEY (DocumentId);

ALTER TABLE docflow.tbl_FileVersions
ADD CONSTRAINT PK_FileVersions PRIMARY KEY (VersionId);

ALTER TABLE docflow.tbl_WorkflowMovements
ADD CONSTRAINT PK_WorkflowMovements PRIMARY KEY (MovementId);

-- =========================
-- FOREIGN KEYS
-- =========================

-- ProcessHierarchy self-reference
ALTER TABLE docflow.tbl_ProcessHierarchy
ADD CONSTRAINT FK_ProcessHierarchy_Parent
FOREIGN KEY (ParentId) REFERENCES docflow.tbl_ProcessHierarchy(ProcessId);

-- -- Auditoría ProcessHierarchy
-- ALTER TABLE docflow.tbl_ProcessHierarchy
-- ADD CONSTRAINT FK_ProcessHierarchy_CreatedBy
-- FOREIGN KEY (CreatedBy) REFERENCES docflow.tbl_Users(UserId);

-- ALTER TABLE docflow.tbl_ProcessHierarchy
-- ADD CONSTRAINT FK_ProcessHierarchy_UpdatedBy
-- FOREIGN KEY (UpdatedBy) REFERENCES docflow.tbl_Users(UserId);

-- Transiciones
ALTER TABLE docflow.tbl_ProcessTransitions
ADD CONSTRAINT FK_Transitions_FromProcess
FOREIGN KEY (FromProcessId) REFERENCES docflow.tbl_ProcessHierarchy(ProcessId);

ALTER TABLE docflow.tbl_ProcessTransitions
ADD CONSTRAINT FK_Transitions_ToProcess
FOREIGN KEY (ToProcessId) REFERENCES docflow.tbl_ProcessHierarchy(ProcessId);

ALTER TABLE docflow.tbl_ProcessTransitions
ADD CONSTRAINT FK_Transitions_ReturnToProcess
FOREIGN KEY (ReturnToProcessId) REFERENCES docflow.tbl_ProcessHierarchy(ProcessId);

-- ALTER TABLE docflow.tbl_ProcessTransitions
-- ADD CONSTRAINT FK_Transitions_CreatedBy
-- FOREIGN KEY (CreatedBy) REFERENCES docflow.tbl_Users(UserId);

-- ALTER TABLE docflow.tbl_ProcessTransitions
-- ADD CONSTRAINT FK_Transitions_UpdatedBy
-- FOREIGN KEY (UpdatedBy) REFERENCES docflow.tbl_Users(UserId);

-- DocumentRules
ALTER TABLE docflow.tbl_DocumentRules
ADD CONSTRAINT FK_DocRules_Process
FOREIGN KEY (ProcessId) REFERENCES docflow.tbl_ProcessHierarchy(ProcessId);

-- ALTER TABLE docflow.tbl_DocumentRules
-- ADD CONSTRAINT FK_DocRules_CreatedBy
-- FOREIGN KEY (CreatedBy) REFERENCES docflow.tbl_Users(UserId);

-- ALTER TABLE docflow.tbl_DocumentRules
-- ADD CONSTRAINT FK_DocRules_UpdatedBy
-- FOREIGN KEY (UpdatedBy) REFERENCES docflow.tbl_Users(UserId);

-- Instances
ALTER TABLE docflow.tbl_WorkflowInstances
ADD CONSTRAINT FK_Instances_Process
FOREIGN KEY (ProcessId) REFERENCES docflow.tbl_ProcessHierarchy(ProcessId);

-- ALTER TABLE docflow.tbl_WorkflowInstances
-- ADD CONSTRAINT FK_Instances_AssignedTo
-- FOREIGN KEY (AssignedToUserId) REFERENCES docflow.tbl_Users(UserId);

-- ALTER TABLE docflow.tbl_WorkflowInstances
-- ADD CONSTRAINT FK_Instances_CreatedBy
-- FOREIGN KEY (CreatedBy) REFERENCES docflow.tbl_Users(UserId);

-- ALTER TABLE docflow.tbl_WorkflowInstances
-- ADD CONSTRAINT FK_Instances_UpdatedBy
-- FOREIGN KEY (UpdatedBy) REFERENCES docflow.tbl_Users(UserId);

-- Documents
ALTER TABLE docflow.tbl_Documents
ADD CONSTRAINT FK_Documents_Instance
FOREIGN KEY (InstanceId) REFERENCES docflow.tbl_WorkflowInstances(InstanceId);

ALTER TABLE docflow.tbl_Documents
ADD CONSTRAINT FK_Documents_Rule
FOREIGN KEY (RuleId) REFERENCES docflow.tbl_DocumentRules(RuleId);

-- ALTER TABLE docflow.tbl_Documents
-- ADD CONSTRAINT FK_Documents_CreatedBy
-- FOREIGN KEY (CreatedBy) REFERENCES docflow.tbl_Users(UserId);

-- ALTER TABLE docflow.tbl_Documents
-- ADD CONSTRAINT FK_Documents_UpdatedBy
-- FOREIGN KEY (UpdatedBy) REFERENCES docflow.tbl_Users(UserId);

-- FileVersions
ALTER TABLE docflow.tbl_FileVersions
ADD CONSTRAINT FK_FileVersions_Document
FOREIGN KEY (DocumentId) REFERENCES docflow.tbl_Documents(DocumentId);

-- ALTER TABLE docflow.tbl_FileVersions
-- ADD CONSTRAINT FK_FileVersions_CreatedBy
-- FOREIGN KEY (CreatedBy) REFERENCES docflow.tbl_Users(UserId);

-- Movements
ALTER TABLE docflow.tbl_WorkflowMovements
ADD CONSTRAINT FK_Movements_Instance
FOREIGN KEY (InstanceId) REFERENCES docflow.tbl_WorkflowInstances(InstanceId);

-- ALTER TABLE docflow.tbl_WorkflowMovements
-- ADD CONSTRAINT FK_Movements_AssignedTo
-- FOREIGN KEY (AssignedToUserId) REFERENCES docflow.tbl_Users(UserId);

-- ALTER TABLE docflow.tbl_WorkflowMovements
-- ADD CONSTRAINT FK_Movements_CreatedBy
-- FOREIGN KEY (CreatedBy) REFERENCES docflow.tbl_Users(UserId);

-- =========================
-- CHECK CONSTRAINTS
-- =========================

ALTER TABLE docflow.tbl_DocumentRules
ADD CONSTRAINT CK_DocRules_DefaultVisibility
CHECK (DefaultVisibility IN (1,2));

ALTER TABLE docflow.tbl_Documents
ADD CONSTRAINT CK_Documents_Visibility
CHECK (Visibility IN (1,2));

ALTER TABLE docflow.tbl_WorkflowMovements
ADD CONSTRAINT CK_Movements_MovementType
CHECK (MovementType IN ('FORWARD','RETURN'));

ALTER TABLE docflow.tbl_WorkflowInstances
ADD CONSTRAINT CK_Instances_CurrentStatus_NotEmpty
CHECK (LEN(LTRIM(RTRIM(CurrentStatus))) > 0);

-- =========================
-- UNIQUE / INDEX
-- =========================

-- -- Usuarios: username único
-- CREATE UNIQUE INDEX UX_Users_Username
-- ON docflow.tbl_Users(Username);

-- Procesos: código único + búsquedas por jerarquía y depto responsable
CREATE UNIQUE INDEX UX_Process_Code
ON docflow.tbl_ProcessHierarchy(ProcessCode);

CREATE INDEX IX_Process_Parent
ON docflow.tbl_ProcessHierarchy(ParentId);

CREATE INDEX IX_Process_ResponsibleDept
ON docflow.tbl_ProcessHierarchy(ResponsibleDepartmentId);

-- Transiciones: resolver siguiente proceso
CREATE UNIQUE INDEX UX_Transitions_From_To
ON docflow.tbl_ProcessTransitions(FromProcessId, ToProcessId);

CREATE INDEX IX_Transitions_From_Default
ON docflow.tbl_ProcessTransitions(FromProcessId, IsDefault);

-- Reglas: lookup por proceso
CREATE INDEX IX_DocRules_Process
ON docflow.tbl_DocumentRules(ProcessId);

CREATE INDEX IX_DocRules_Process_Required
ON docflow.tbl_DocumentRules(ProcessId, IsRequired);

-- Expedientes: bandejas por depto actual y estado
CREATE INDEX IX_Instances_CurrentDept_Status
ON docflow.tbl_WorkflowInstances(CurrentDepartmentId, CurrentStatus, CreatedAt DESC);

CREATE INDEX IX_Instances_Process
ON docflow.tbl_WorkflowInstances(ProcessId);

-- Documentos: filtro por visibilidad + depto creador, excluyendo borrados
CREATE INDEX IX_Documents_Instance_Visibility_Dept
ON docflow.tbl_Documents(InstanceId, Visibility, CreatedByDepartmentId)
WHERE IsDeleted = 0;

CREATE INDEX IX_Documents_Rule
ON docflow.tbl_Documents(RuleId);

-- Versiones: una única versión por (documento, número)
CREATE UNIQUE INDEX UX_FileVersions_Document_Version
ON docflow.tbl_FileVersions(DocumentId, VersionNumber);

-- Movimientos: historial por expediente y auditoría
CREATE INDEX IX_Movements_Instance_CreatedAt
ON docflow.tbl_WorkflowMovements(InstanceId, CreatedAt DESC);

CREATE INDEX IX_Movements_FromToProcess
ON docflow.tbl_WorkflowMovements(FromProcessId, ToProcessId);

CREATE INDEX IX_Movements_FromToDept
ON docflow.tbl_WorkflowMovements(FromDepartmentId, ToDepartmentId);
GO

CREATE INDEX IX_WorkflowInstances_RootProcessId 
ON docflow.tbl_WorkflowInstances(RootProcessId);
GO