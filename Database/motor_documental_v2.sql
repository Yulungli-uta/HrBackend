-- ================================================================
-- Motor Documental Institucional — Script ajustado para EF Core
-- ================================================================
-- IMPORTANTE: columnas renombradas respecto al diseño conceptual
-- para alinear con los nombres que EF Core espera según los
-- modelos C# existentes (convención + HasColumnName explícito).
--
-- Cambios de nomenclatura aplicados:
--   DocumentTemplateID   → TemplateID
--   DocumentTemplateTypeID/VersionNumber → TemplateType+Version (string)
--   BodyTemplate/HeaderTemplate/FooterTemplate → HtmlContent+CssStyles+MetaJson
--   DocumentTemplateFieldID → FieldID
--   FieldCode → FieldName  |  FieldLabel → Label  |  SourcePath → SourceProperty
--   GeneratedDocumentID  → DocumentID
--   GeneratedDocumentFieldID → DocumentFieldID
--   FieldCode → FieldName  |  RawValue/RenderedValue → FieldValue+WasOverridden
--   PersonnelActionID    → ActionID
--   StatusTypeID (FK)    → Status (NVARCHAR, sin FK a ref_Types)
--   DestDepartmentID/DestJobID → DestinationDepartmentId/DestinationJobId
-- ================================================================


-- ================================================================
-- BLOQUE 1: SEEDS EN ref_Types
-- ================================================================

INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, SortOrder, CreatedBy)
VALUES
-- Familias documentales del motor genérico
('DOCUMENT_TEMPLATE_TYPE', 'Contrato',           'Plantillas para contratos laborales bajo cualquier régimen',        1, 10, NULL),
('DOCUMENT_TEMPLATE_TYPE', 'Acción de Personal', 'Plantillas para acciones de personal: traslados, comisiones, etc.', 1, 20, NULL),
('DOCUMENT_TEMPLATE_TYPE', 'Convenio',           'Plantillas para convenios interinstitucionales',                    1, 30, NULL),
('DOCUMENT_TEMPLATE_TYPE', 'Oficio',             'Plantillas para oficios internos y externos',                       1, 40, NULL),

-- Estados de ciclo de vida del documento emitido
('GENERATED_DOC_STATUS', 'Generado',  'Documento renderizado y guardado, pendiente de firma o revisión', 1, 10, NULL),
('GENERATED_DOC_STATUS', 'Firmado',   'Documento con firma electrónica o física aplicada',               1, 20, NULL),
('GENERATED_DOC_STATUS', 'Anulado',   'Documento sin validez legal, reemplazado o revocado',             1, 30, NULL),
('GENERATED_DOC_STATUS', 'Archivado', 'Documento en repositorio histórico, ya sin vigencia operativa',   1, 40, NULL),

-- Estados del ciclo de vida de la acción de personal
('PERSONNEL_ACTION_STATUS', 'Borrador',   'Acción en elaboración, aún no tramitada',                1, 10, NULL),
('PERSONNEL_ACTION_STATUS', 'Aprobada',   'Acción aprobada por la autoridad competente',            1, 20, NULL),
('PERSONNEL_ACTION_STATUS', 'Ejecutada',  'Acción que ya produjo efecto operativo en el sistema',   1, 30, NULL),
('PERSONNEL_ACTION_STATUS', 'Anulada',    'Acción sin efecto, revocada antes o después de ejecución', 1, 40, NULL);


-- ================================================================
-- BLOQUE 2: TABLAS NUEVAS
-- ================================================================

-- ----------------------------------------------------------------
-- 2.1 HR.tbl_DocumentTemplates
-- ----------------------------------------------------------------
-- Almacena las plantillas HTML del motor documental institucional.
-- TemplateType almacena el código de familia como string (ej: 'ACCION_PERSONAL')
-- en lugar de FK a ref_Types, para simplificar la resolución en el renderer.
-- Version es string semántico (ej: '1.0', '2.1') en lugar de entero.
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_DocumentTemplates (
    TemplateID          INT             NOT NULL IDENTITY(1,1),
    TemplateCode        NVARCHAR(50)    NOT NULL,
    Name                NVARCHAR(150)   NOT NULL,
    Description         NVARCHAR(500)   NULL,
    TemplateType        NVARCHAR(50)    NOT NULL,
    Version             NVARCHAR(10)    NOT NULL    DEFAULT('1.0'),
    LayoutType          NVARCHAR(20)    NOT NULL    DEFAULT('FLOW_TEXT'),
    Status              NVARCHAR(20)    NOT NULL    DEFAULT('DRAFT'),
    HtmlContent         NVARCHAR(MAX)   NOT NULL,
    CssStyles           NVARCHAR(MAX)   NULL,
    MetaJson            NVARCHAR(MAX)   NULL,
    RequiresSignature   BIT             NOT NULL    DEFAULT(0),
    RequiresApproval    BIT             NOT NULL    DEFAULT(0),
    CreatedAt           DATETIME2       NULL,
    CreatedBy           INT             NULL,
    UpdatedAt           DATETIME2       NULL,
    UpdatedBy           INT             NULL
);


-- ----------------------------------------------------------------
-- 2.2 HR.tbl_DocumentTemplateFields
-- ----------------------------------------------------------------
-- Declara los placeholders {{CAMPO}} de cada plantilla.
-- DefaultValue: valor de respaldo cuando el campo no puede resolverse.
-- IsEditable: indica si el usuario puede sobreescribir el valor en UI.
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_DocumentTemplateFields (
    FieldID             INT             NOT NULL IDENTITY(1,1),
    TemplateID          INT             NOT NULL,
    FieldName           NVARCHAR(100)   NOT NULL,
    Label               NVARCHAR(150)   NOT NULL,
    SourceType          NVARCHAR(20)    NOT NULL    DEFAULT('SYSTEM'),
    SourceProperty      NVARCHAR(200)   NULL,
    DataType            NVARCHAR(20)    NOT NULL    DEFAULT('TEXT'),
    FormatPattern       NVARCHAR(50)    NULL,
    DefaultValue        NVARCHAR(500)   NULL,
    IsRequired          BIT             NOT NULL    DEFAULT(0),
    IsEditable          BIT             NOT NULL    DEFAULT(0),
    SortOrder           INT             NOT NULL    DEFAULT(0),
    HelpText            NVARCHAR(300)   NULL,
    CreatedAt           DATETIME2       NULL,
    CreatedBy           INT             NULL,
    UpdatedAt           DATETIME2       NULL,
    UpdatedBy           INT             NULL
);


-- ----------------------------------------------------------------
-- 2.3 HR.tbl_GeneratedDocuments
-- ----------------------------------------------------------------
-- Snapshot inmutable del documento institucional emitido.
-- Status como string directo (DRAFT/GENERATED/SIGNED/APPROVED/REJECTED/ARCHIVED).
-- FileName: nombre del archivo PDF guardado en tbl_StoredFile.
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_GeneratedDocuments (
    DocumentID          INT             NOT NULL IDENTITY(1,1),
    TemplateID          INT             NOT NULL,
    EmployeeID          INT             NOT NULL,
    EntityType          NVARCHAR(30)    NOT NULL,
    EntityId            INT             NULL,
    DocumentNumber      NVARCHAR(50)    NULL,
    FileName            NVARCHAR(255)   NOT NULL,
    StoredFileID        INT             NULL,
    Status              NVARCHAR(20)    NOT NULL    DEFAULT('DRAFT'),
    Notes               NVARCHAR(1000)  NULL,
    IsSigned            BIT             NOT NULL    DEFAULT(0),
    SignedAt            DATETIME2       NULL,
    SignedBy            INT             NULL,
    IsApproved          BIT             NOT NULL    DEFAULT(0),
    ApprovedAt          DATETIME2       NULL,
    ApprovedBy          INT             NULL,
    CreatedAt           DATETIME2       NULL,
    CreatedBy           INT             NULL,
    UpdatedAt           DATETIME2       NULL,
    UpdatedBy           INT             NULL
);


-- ----------------------------------------------------------------
-- 2.4 HR.tbl_GeneratedDocumentFields
-- ----------------------------------------------------------------
-- Snapshot de valores de cada placeholder al momento de emitir.
-- WasOverridden: indica si el valor fue ingresado manualmente.
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_GeneratedDocumentFields (
    DocumentFieldID     INT             NOT NULL IDENTITY(1,1),
    DocumentID          INT             NOT NULL,
    FieldName           NVARCHAR(100)   NOT NULL,
    FieldValue          NVARCHAR(MAX)   NULL,
    SourceType          NVARCHAR(20)    NOT NULL    DEFAULT('SYSTEM'),
    WasOverridden       BIT             NOT NULL    DEFAULT(0)
);


-- ----------------------------------------------------------------
-- 2.5 HR.tbl_PersonnelActions
-- ----------------------------------------------------------------
-- Acto administrativo LOSEP/RLOSEP como entidad operativa.
-- Status como string (DRAFT/APPROVED/EXECUTED/CANCELLED).
-- DestinationDepartmentId/DestinationJobId: convención C# sin sufijo ID mayúscula.
-- ContractID es nullable (la acción puede no tener contrato aún).
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_PersonnelActions (
    ActionID                INT             NOT NULL IDENTITY(1,1),
    EmployeeID              INT             NOT NULL,
    ActionTypeID            INT             NOT NULL,
    ContractID              INT             NULL,
    ActionNumber            NVARCHAR(50)    NULL,
    ActionDate              DATE            NOT NULL,
    EffectiveDate           DATE            NULL,
    EndDate                 DATE            NULL,
    OriginDepartmentId      INT             NULL,
    OriginJobId             INT             NULL,
    OriginBudgetCode        NVARCHAR(50)    NULL,
    DestinationDepartmentId INT             NULL,
    DestinationJobId        INT             NULL,
    DestinationBudgetCode   NVARCHAR(50)    NULL,
    PreviousRmu             DECIMAL(10,2)   NULL,
    NewRmu                  DECIMAL(10,2)   NULL,
    LegalBasis              NVARCHAR(500)   NULL,
    Reason                  NVARCHAR(1000)  NULL,
    Observations            NVARCHAR(1000)  NULL,
    Status                  NVARCHAR(20)    NOT NULL    DEFAULT('DRAFT'),
    GeneratedDocumentID     INT             NULL,
    MovementID              INT             NULL,
    CreatedAt               DATETIME2       NULL,
    CreatedBy               INT             NULL,
    UpdatedAt               DATETIME2       NULL,
    UpdatedBy               INT             NULL
);


CREATE TABLE HR.tbl_PersonnelActionStatusHistory (
      HistoryId    INT          NOT NULL IDENTITY(1,1),
      ActionId     INT          NOT NULL,
      FromStatus   NVARCHAR(30) NULL,
      ToStatus     NVARCHAR(30) NOT NULL,
      ChangedAt    DATETIME2    NOT NULL DEFAULT(GETDATE()),
      ChangedBy    INT          NULL,
      Notes        NVARCHAR(500) NULL,
      CONSTRAINT PK_PersonnelActionStatusHistory PRIMARY KEY (HistoryId),
      CONSTRAINT FK_PersonnelActionStatusHistory_Action
          FOREIGN KEY (ActionId) REFERENCES HR.tbl_PersonnelActions (ActionID)
  );


-- ================================================================
-- BLOQUE 3: ALTER TABLE EN TABLAS EXISTENTES
-- ================================================================

-- ----------------------------------------------------------------
-- 3.1 HR.tbl_contract_type
-- DocumentTemplateTypeID: familia documental asociada a este tipo (ref_Types).
-- DefaultTemplateID: plantilla activa por defecto para generar contratos de este tipo.
-- ----------------------------------------------------------------
ALTER TABLE HR.tbl_contract_type
    ADD DocumentTemplateTypeID  INT NULL,
        DefaultTemplateID       INT NULL;


-- ----------------------------------------------------------------
-- 3.2 HR.tbl_Contracts
-- GeneratedDocumentID: enlace al snapshot del documento emitido.
-- TemplateVersionUsed: versión de plantilla al momento de generación.
-- IsDocumentFrozen: 1 = documento emitido y congelado, no regenerar.
-- ----------------------------------------------------------------
ALTER TABLE HR.tbl_Contracts
    ADD GeneratedDocumentID     INT NULL,
        TemplateVersionUsed     INT NULL,
        IsDocumentFrozen        BIT NOT NULL DEFAULT(0);

ALTER TABLE HR.tbl_Contracts
  ADD SignedDocumentStoredFileId INT NULL;

ALTER TABLE HR.tbl_Contracts
  ADD CONSTRAINT FK_Contract_SignedFile
  FOREIGN KEY (SignedDocumentStoredFileId) REFERENCES HR.tbl_StoredFiles (StoredFileID);

-- ----------------------------------------------------------------
-- 3.3 HR.tbl_PersonnelMovements
-- PersonnelActionID: referencia a la acción de personal que originó el movimiento.
-- ----------------------------------------------------------------
ALTER TABLE HR.tbl_PersonnelMovements
    ADD PersonnelActionID       INT NULL;

ALTER TABLE HR.tbl_PersonnelActions
  ADD SignedDocumentStoredFileId INT NULL;

ALTER TABLE HR.tbl_PersonnelActions
  ADD CONSTRAINT FK_PersonnelActions_SignedFile
  FOREIGN KEY (SignedDocumentStoredFileId) REFERENCES HR.tbl_StoredFiles (StoredFileID);
  
ALTER TABLE HR.tbl_PersonnelActions
  ADD StatusTypeId INT NULL;
  
ALTER TABLE HR.tbl_PersonnelActions
  ADD CONSTRAINT FK_PersonnelActions_StatusTypeId
  FOREIGN KEY (StatusTypeId) REFERENCES HR.ref_Types (TypeID);

ALTER TABLE HR.tbl_GeneratedDocuments
  ADD TemplateVersion  NVARCHAR(10)  NULL,
	  HtmlSnapshot     NVARCHAR(MAX) NULL;
-- ================================================================
-- BLOQUE 4: PRIMARY KEYS
-- ================================================================

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT PK_DocumentTemplates PRIMARY KEY (TemplateID);

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT PK_DocumentTemplateFields PRIMARY KEY (FieldID);

ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT PK_GeneratedDocuments PRIMARY KEY (DocumentID);

ALTER TABLE HR.tbl_GeneratedDocumentFields
    ADD CONSTRAINT PK_GeneratedDocumentFields PRIMARY KEY (DocumentFieldID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT PK_PersonnelActions PRIMARY KEY (ActionID);


-- ================================================================
-- BLOQUE 5: UNIQUE CONSTRAINTS
-- ================================================================

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT UQ_DocumentTemplates_TemplateCode UNIQUE (TemplateCode);

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT UQ_DocumentTemplateFields_TemplateFieldName
    UNIQUE (TemplateID, FieldName);

CREATE UNIQUE INDEX UQ_PersonnelActions_ActionNumber
    ON HR.tbl_PersonnelActions (ActionNumber)
    WHERE ActionNumber IS NOT NULL;


-- ================================================================
-- BLOQUE 6: CHECK CONSTRAINTS
-- ================================================================

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT CHK_DocumentTemplates_LayoutType
    CHECK (LayoutType IN ('FLOW_TEXT', 'STRUCTURED_FORM', 'HYBRID'));

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT CHK_DocumentTemplates_Status
    CHECK (Status IN ('DRAFT', 'PUBLISHED', 'ARCHIVED'));

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT CHK_DocumentTemplateFields_DataType
    CHECK (DataType IN ('TEXT', 'DATE', 'NUMBER', 'BOOLEAN', 'CURRENCY'));

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT CHK_DocumentTemplateFields_SourceType
    CHECK (SourceType IN ('SYSTEM', 'EMPLOYEE', 'CONTRACT', 'MOVEMENT', 'MANUAL'));

ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT CHK_GeneratedDocuments_EntityType
    CHECK (EntityType IN ('CONTRACT', 'PERSONNELACTION', 'AGREEMENT', 'OFICIO'));

ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT CHK_GeneratedDocuments_Status
    CHECK (Status IN ('DRAFT', 'GENERATED', 'SIGNED', 'APPROVED', 'REJECTED', 'ARCHIVED'));

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT CHK_PersonnelActions_Dates
    CHECK (EndDate IS NULL OR EndDate >= EffectiveDate);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT CHK_PersonnelActions_Status
    CHECK (Status IN ('BORRADOR', 'GENERADO', 'PENDIENTE_FIRMAS', 'FIRMADO_CARGADO', 'FINALIZADO','ANULADO'));


-- ================================================================
-- BLOQUE 7: FOREIGN KEYS
-- ================================================================

-- ---- tbl_DocumentTemplates ----
ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT FK_DocumentTemplates_CreatedBy
    FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT FK_DocumentTemplates_UpdatedBy
    FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

-- ---- tbl_DocumentTemplateFields ----
ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT FK_DocumentTemplateFields_Template
    FOREIGN KEY (TemplateID) REFERENCES HR.tbl_DocumentTemplates (TemplateID);

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT FK_DocumentTemplateFields_CreatedBy
    FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT FK_DocumentTemplateFields_UpdatedBy
    FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

-- ---- tbl_GeneratedDocuments ----
ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT FK_GeneratedDocuments_Template
    FOREIGN KEY (TemplateID) REFERENCES HR.tbl_DocumentTemplates (TemplateID);

ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT FK_GeneratedDocuments_Employee
    FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees (EmployeeID);

ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT FK_GeneratedDocuments_StoredFile
    FOREIGN KEY (StoredFileID) REFERENCES HR.TBL_StoredFile (FileId);

ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT FK_GeneratedDocuments_CreatedBy
    FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

-- ---- tbl_GeneratedDocumentFields ----
ALTER TABLE HR.tbl_GeneratedDocumentFields
    ADD CONSTRAINT FK_GeneratedDocumentFields_Document
    FOREIGN KEY (DocumentID) REFERENCES HR.tbl_GeneratedDocuments (DocumentID);

-- ---- tbl_PersonnelActions ----
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_Employee
    FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees (EmployeeID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_ActionType
    FOREIGN KEY (ActionTypeID) REFERENCES HR.ref_Types (TypeID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_Contract
    FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts (ContractID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_GeneratedDocument
    FOREIGN KEY (GeneratedDocumentID) REFERENCES HR.tbl_GeneratedDocuments (DocumentID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_Movement
    FOREIGN KEY (MovementID) REFERENCES HR.tbl_PersonnelMovements (MovementID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_CreatedBy
    FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_UpdatedBy
    FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

-- ---- tbl_contract_type (columnas nuevas) ----
ALTER TABLE HR.tbl_contract_type
    ADD CONSTRAINT FK_contract_type_DocumentTemplateType
    FOREIGN KEY (DocumentTemplateTypeID) REFERENCES HR.ref_Types (TypeID);

ALTER TABLE HR.tbl_contract_type
    ADD CONSTRAINT FK_contract_type_DefaultTemplate
    FOREIGN KEY (DefaultTemplateID) REFERENCES HR.tbl_DocumentTemplates (TemplateID);

-- ---- tbl_Contracts (columna nueva) ----
ALTER TABLE HR.tbl_Contracts
    ADD CONSTRAINT FK_Contracts_GeneratedDocument
    FOREIGN KEY (GeneratedDocumentID) REFERENCES HR.tbl_GeneratedDocuments (DocumentID);

-- ---- tbl_PersonnelMovements (columna nueva) ----
ALTER TABLE HR.tbl_PersonnelMovements
    ADD CONSTRAINT FK_PersonnelMovements_PersonnelAction
    FOREIGN KEY (PersonnelActionID) REFERENCES HR.tbl_PersonnelActions (ActionID);


-- ================================================================
-- BLOQUE 8: ÍNDICES DE PERFORMANCE
-- ================================================================

-- Plantillas: búsqueda por tipo y estado (más frecuente al generar)
CREATE NONCLUSTERED INDEX IX_DocumentTemplates_TypeStatus
ON HR.tbl_DocumentTemplates (TemplateType, Status)
INCLUDE (TemplateID, TemplateCode, Version, LayoutType, RequiresSignature, RequiresApproval);

-- Campos: carga de todos los placeholders de una plantilla
CREATE NONCLUSTERED INDEX IX_DocumentTemplateFields_Template
ON HR.tbl_DocumentTemplateFields (TemplateID, SortOrder)
INCLUDE (FieldName, Label, SourceType, SourceProperty, DataType, IsRequired, IsEditable, DefaultValue, FormatPattern);

-- Documentos generados: historial por empleado y entidad
CREATE NONCLUSTERED INDEX IX_GeneratedDocuments_Employee
ON HR.tbl_GeneratedDocuments (EmployeeID, CreatedAt DESC)
INCLUDE (DocumentID, TemplateID, EntityType, EntityId, DocumentNumber, Status, StoredFileID);

CREATE NONCLUSTERED INDEX IX_GeneratedDocuments_Entity
ON HR.tbl_GeneratedDocuments (EntityType, EntityId, CreatedAt DESC)
INCLUDE (DocumentID, TemplateID, DocumentNumber, Status, StoredFileID);

-- Campos de documento: reconstrucción de snapshot por documento
CREATE NONCLUSTERED INDEX IX_GeneratedDocumentFields_Document
ON HR.tbl_GeneratedDocumentFields (DocumentID)
INCLUDE (FieldName, FieldValue, SourceType, WasOverridden);

-- Acciones de personal: por empleado y estado
CREATE NONCLUSTERED INDEX IX_PersonnelActions_EmployeeStatus
ON HR.tbl_PersonnelActions (EmployeeID, Status, ActionDate DESC)
INCLUDE (ActionID, ActionTypeID, ContractID, ActionNumber, GeneratedDocumentID);

-- Acciones: por contrato
CREATE NONCLUSTERED INDEX IX_PersonnelActions_Contract
ON HR.tbl_PersonnelActions (ContractID, ActionDate DESC)
INCLUDE (ActionID, ActionTypeID, Status, ActionNumber);
