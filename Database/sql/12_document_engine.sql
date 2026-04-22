Tengo todo el contexto necesario. Aquí está el SQL definitivo completo con descripciones de cada tabla, campo, constraint e índice:

-- ================================================================
-- BLOQUE 1: SEEDS EN ref_Types
-- ----------------------------------------------------------------
-- Propósito: Registrar las categorías nuevas que necesita el motor
-- genérico documental, reutilizando la infraestructura existente
-- de ref_Types en lugar de crear tbl_DocumentTemplateTypes.
--
-- DOCUMENT_TEMPLATE_TYPE: Familias documentales soportadas por el
--   motor. Cada tipo agrupa plantillas del mismo formato institucional.
--
-- GENERATED_DOC_STATUS: Estados del ciclo de vida de un documento
--   ya emitido. Complementa la trazabilidad del snapshot.
--
-- PERSONNEL_ACTION_STATUS: Estados del ciclo de vida de una acción
--   de personal como entidad operativa independiente.
-- ================================================================

INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, SortOrder, CreatedBy)
VALUES
-- Familias documentales del motor genérico
('DOCUMENT_TEMPLATE_TYPE', 'Contrato',           'Plantillas para contratos laborales bajo cualquier régimen',       1, 10, NULL),
('DOCUMENT_TEMPLATE_TYPE', 'Acción de Personal', 'Plantillas para acciones de personal: traslados, comisiones, etc.', 1, 20, NULL),
('DOCUMENT_TEMPLATE_TYPE', 'Convenio',           'Plantillas para convenios interinstitucionales',                   1, 30, NULL),
('DOCUMENT_TEMPLATE_TYPE', 'Oficio',             'Plantillas para oficios internos y externos',                      1, 40, NULL),

-- Estados de ciclo de vida del documento emitido (snapshot)
('GENERATED_DOC_STATUS', 'Generado',  'Documento renderizado y guardado, pendiente de firma o revisión', 1, 10, NULL),
('GENERATED_DOC_STATUS', 'Firmado',   'Documento con firma electrónica o física aplicada',               1, 20, NULL),
('GENERATED_DOC_STATUS', 'Anulado',   'Documento sin validez legal, reemplazado o revocado',             1, 30, NULL),
('GENERATED_DOC_STATUS', 'Archivado', 'Documento en repositorio histórico, ya sin vigencia operativa',   1, 40, NULL),

-- Estados del ciclo de vida de la acción de personal
('PERSONNEL_ACTION_STATUS', 'Borrador',   'Acción en elaboración, aún no tramitada',                   1, 10, NULL),
('PERSONNEL_ACTION_STATUS', 'Aprobada',   'Acción aprobada por la autoridad competente',               1, 20, NULL),
('PERSONNEL_ACTION_STATUS', 'Ejecutada',  'Acción que ya produjo efecto operativo en el sistema',      1, 30, NULL),
('PERSONNEL_ACTION_STATUS', 'Anulada',    'Acción sin efecto, revocada antes o después de ejecución',  1, 40, NULL);

-- ================================================================
-- BLOQUE 2: TABLAS NUEVAS
-- ================================================================


-- ----------------------------------------------------------------
-- 2.1 HR.tbl_DocumentTemplates
-- ----------------------------------------------------------------
-- Propósito: Almacena las plantillas versionadas del motor genérico
-- de documentos institucionales. Cada fila representa una versión
-- específica de un formato (contrato, acción de personal, convenio,
-- oficio u otro). Una plantilla no se modifica una vez publicada:
-- los cambios producen una nueva versión con número incrementado.
--
-- Campos clave:
--   DocumentTemplateTypeID : Familia documental (ref_Types DOCUMENT_TEMPLATE_TYPE)
--   TemplateCode           : Identificador de familia de formato, ej: 'CONTRATO_NOMBRAMIENTO'
--                            Agrupa todas las versiones de un mismo formato
--   VersionNumber          : Número de versión dentro del mismo TemplateCode (inicia en 1)
--   LayoutType             : Determina el renderer a usar en el backend:
--                              FLOW_TEXT       → contratos, convenios, párrafos corridos
--                              STRUCTURED_FORM → acciones de personal, formularios con campos fijos
--                              HYBRID          → mezcla de secciones narrativas y campos tabulares
--   Status                 : Ciclo de vida de la plantilla:
--                              DRAFT     → en elaboración, no genera documentos
--                              PUBLISHED → activa, usada para emisión. Solo una por TemplateCode
--                              ARCHIVED  → reemplazada por versión nueva, solo lectura
--   BodyTemplate           : Cuerpo principal con placeholders nombrados {{FieldCode}}
--   HeaderTemplate         : Encabezado institucional reutilizable (logo, nombre entidad, etc.)
--   FooterTemplate         : Pie de página con numeración, firma, etc.
--   EffectiveFrom/To       : Vigencia temporal. El motor selecciona la plantilla vigente
--                            en la fecha de emisión, no necesariamente la última
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_DocumentTemplates (
    DocumentTemplateID      INT             NOT NULL IDENTITY(1,1),
    DocumentTemplateTypeID  INT             NOT NULL,
    TemplateCode            NVARCHAR(50)    NOT NULL,
    Name                    NVARCHAR(200)   NOT NULL,
    VersionNumber           INT             NOT NULL    DEFAULT(1),
    LayoutType              NVARCHAR(30)    NOT NULL,
    Status                  NVARCHAR(20)    NOT NULL    DEFAULT('DRAFT'),
    BodyTemplate            NVARCHAR(MAX)   NOT NULL,
    HeaderTemplate          NVARCHAR(MAX)   NULL,
    FooterTemplate          NVARCHAR(MAX)   NULL,
    EffectiveFrom           DATETIME2       NULL,
    EffectiveTo             DATETIME2       NULL,
    IsActive                BIT             NOT NULL    DEFAULT(1),
    CreatedAt               DATETIME2       NOT NULL    DEFAULT(GETDATE()),
    CreatedBy               INT             NULL,
    UpdatedAt               DATETIME2       NULL,
    UpdatedBy               INT             NULL
);


-- ----------------------------------------------------------------
-- 2.2 HR.tbl_DocumentTemplateFields
-- ----------------------------------------------------------------
-- Propósito: Declara formalmente los placeholders que usa cada
-- versión de plantilla. Permite al motor saber exactamente qué
-- datos necesita resolver antes de renderizar el documento, y
-- permite validar que todos los valores estén disponibles antes
-- de emitir.
--
-- Campos clave:
--   FieldCode     : Nombre del placeholder tal como aparece en la plantilla:
--                   {{EmployeeFullName}}, {{StartDate}}, {{ResolutionNumber}}
--   FieldLabel    : Etiqueta legible para UI de configuración y validación
--   DataType      : Tipo lógico del valor esperado:
--                     TEXT / DATE / NUMBER / BOOLEAN / CURRENCY
--   IsRequired    : Si es 1, el motor no genera el documento sin este valor
--   SourceType    : Origen del dato para resolución automática:
--                     SYSTEM    → calculado por el motor (fecha actual, número de documento)
--                     EMPLOYEE  → viene del registro del empleado
--                     CONTRACT  → viene de tbl_Contracts
--                     MOVEMENT  → viene de tbl_PersonnelMovements o tbl_PersonnelActions
--                     MANUAL    → debe ingresarlo el usuario en el momento de emisión
--   SourcePath    : Ruta lógica al dato en el modelo de negocio: 'Employee.FullName'
--   FormatPattern : Patrón de formato aplicado al renderizar: 'dd/MM/yyyy', 'N2', 'UPPERCASE'
--   SortOrder     : Orden de presentación en UI y en el formulario de datos manuales
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_DocumentTemplateFields (
    DocumentTemplateFieldID INT             NOT NULL IDENTITY(1,1),
    DocumentTemplateID      INT             NOT NULL,
    FieldCode               NVARCHAR(100)   NOT NULL,
    FieldLabel              NVARCHAR(150)   NOT NULL,
    DataType                NVARCHAR(30)    NOT NULL,
    IsRequired              BIT             NOT NULL    DEFAULT(0),
    SourceType              NVARCHAR(50)    NULL,
    SourcePath              NVARCHAR(200)   NULL,
    FormatPattern           NVARCHAR(100)   NULL,
    SortOrder               INT             NOT NULL    DEFAULT(0),
    CreatedAt               DATETIME2       NOT NULL    DEFAULT(GETDATE()),
    CreatedBy               INT             NULL,
    UpdatedAt               DATETIME2       NULL,
    UpdatedBy               INT             NULL
);


-- ----------------------------------------------------------------
-- 2.3 HR.tbl_GeneratedDocuments
-- ----------------------------------------------------------------
-- Propósito: Almacena el snapshot inmutable de cada documento
-- institucional emitido. Una vez que el documento se genera y
-- congela, esta fila no cambia aunque luego se modifique la
-- plantilla, los datos del empleado, el contrato o la acción.
-- Es el registro histórico y legal del documento tal como fue
-- entregado o firmado.
--
-- Campos clave:
--   DocumentTemplateID    : Plantilla usada. FK a tbl_DocumentTemplates
--   TemplateVersionNumber : Copia del VersionNumber al momento de emisión.
--                           Se guarda aquí para que el histórico sea
--                           autocontenido aunque la plantilla cambie luego
--   EntityType            : Tipo de entidad operativa que originó el documento:
--                             CONTRACT / PERSONNEL_ACTION / AGREEMENT / OFICIO
--   EntityID              : ID del registro origen en su tabla correspondiente
--   DocumentNumber        : Número oficial del documento (resolución, oficio, etc.)
--   GeneratedFormat       : Formato del archivo físico generado: PDF / HTML / DOCX
--   FinalContent          : HTML o texto renderizado final. Permite reconstruir
--                           el documento sin necesidad del archivo físico
--   FinalContentJson      : Contexto completo de datos usado en la generación,
--                           serializado como JSON. Auditoría de qué valores tenía
--                           el sistema en el momento exacto de emisión
--   StoredFileID          : Referencia al archivo físico en TBL_StoredFile (PDF/DOCX)
--   StatusTypeID          : Estado del documento → ref_Types (GENERATED_DOC_STATUS)
--   GeneratedAt           : Timestamp exacto de generación (no modificable)
--   GeneratedBy           : Empleado que emitió el documento
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_GeneratedDocuments (
    GeneratedDocumentID     INT             NOT NULL IDENTITY(1,1),
    DocumentTemplateID      INT             NOT NULL,
    TemplateVersionNumber   INT             NOT NULL,
    EntityType              NVARCHAR(50)    NOT NULL,
    EntityID                INT             NOT NULL,
    DocumentNumber          NVARCHAR(100)   NULL,
    GeneratedFormat         NVARCHAR(10)    NOT NULL,
    FinalContent            NVARCHAR(MAX)   NULL,
    FinalContentJson        NVARCHAR(MAX)   NULL,
    StoredFileID            INT             NULL,
    StatusTypeID            INT             NULL,
    GeneratedAt             DATETIME2       NOT NULL    DEFAULT(GETDATE()),
    GeneratedBy             INT             NULL
);


-- ----------------------------------------------------------------
-- 2.4 HR.tbl_GeneratedDocumentFields
-- ----------------------------------------------------------------
-- Propósito: Guarda el valor exacto de cada placeholder en el
-- momento en que se generó el documento. Complementa el snapshot
-- de tbl_GeneratedDocuments con granularidad por campo.
-- Permite reconstruir qué dato específico llenó cada placeholder,
-- auditar diferencias entre versiones y facilitar búsquedas por
-- valor (ej: "todos los documentos donde ActionReason = 'Traslado'").
--
-- Campos clave:
--   FieldCode      : Código del placeholder, igual al de tbl_DocumentTemplateFields
--   RawValue       : Valor crudo del origen antes de aplicar FormatPattern
--   RenderedValue  : Valor ya formateado tal como apareció en el documento final
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_GeneratedDocumentFields (
    GeneratedDocumentFieldID    INT             NOT NULL IDENTITY(1,1),
    GeneratedDocumentID         INT             NOT NULL,
    FieldCode                   NVARCHAR(100)   NOT NULL,
    RawValue                    NVARCHAR(MAX)   NULL,
    RenderedValue               NVARCHAR(MAX)   NULL
);


-- ----------------------------------------------------------------
-- 2.5 HR.tbl_PersonnelActions
-- ----------------------------------------------------------------
-- Propósito: Entidad operativa que representa una acción de personal
-- como trámite formal e independiente. No es un movimiento de personal
-- (tbl_PersonnelMovements), sino el acto administrativo que lo origina.
--
-- Distinción clave:
--   tbl_PersonnelActions   → el acto: "se emite acción de traslado"
--   tbl_PersonnelMovements → el efecto: "el empleado quedó en depto X desde fecha Y"
--
-- Una acción puede existir sin movimiento (si fue anulada antes de ejecutarse).
-- Un movimiento puede referenciar la acción que lo originó (MovementID se agrega
-- como columna en tbl_PersonnelMovements en el Bloque 3).
--
-- Campos clave:
--   ActionTypeID       : Tipo de acción → ref_Types (ACTION_TYPE):
--                          TRASLADO / COMISIÓN DE SERVICIOS / ENCARGO / LICENCIA, etc.
--   EmployeeID         : Empleado al que se aplica la acción
--   ContractID         : Contrato vigente al momento de la acción
--   ActionNumber       : Número oficial de la resolución o acción
--   EffectiveDate      : Fecha desde la que la acción tiene efecto operativo
--   EndDate            : Fecha de fin si la acción es temporal (licencias, comisiones)
--   Reason             : Fundamentación o motivo de la acción
--   OriginDepartmentID : Departamento de origen (para traslados, encargos, etc.)
--   DestDepartmentID   : Departamento destino
--   OriginJobID        : Cargo de origen
--   DestJobID          : Cargo destino (para ascensos, cambios de función)
--   StatusTypeID       : Estado → ref_Types (PERSONNEL_ACTION_STATUS)
--   GeneratedDocumentID: Documento emitido para esta acción (snapshot en tbl_GeneratedDocuments)
--   TemplateVersionUsed: Versión de plantilla con la que se generó el documento
--   IsDocumentFrozen   : 1 = documento emitido y congelado, no regenerar automáticamente
--   MovementID         : Movimiento de personal producido como efecto de esta acción.
--                        NULL mientras no se haya ejecutado el efecto operativo
-- ----------------------------------------------------------------
CREATE TABLE HR.tbl_PersonnelActions (
    PersonnelActionID       INT             NOT NULL IDENTITY(1,1),
    ActionTypeID            INT             NOT NULL,
    EmployeeID              INT             NOT NULL,
    ContractID              INT             NOT NULL,
    ActionNumber            NVARCHAR(50)    NULL,
    EffectiveDate           DATE            NOT NULL,
    EndDate                 DATE            NULL,
    Reason                  NVARCHAR(500)   NULL,
    OriginDepartmentID      INT             NULL,
    DestDepartmentID        INT             NULL,
    OriginJobID             INT             NULL,
    DestJobID               INT             NULL,
    StatusTypeID            INT             NOT NULL,
    GeneratedDocumentID     INT             NULL,
    TemplateVersionUsed     INT             NULL,
    IsDocumentFrozen        BIT             NOT NULL    DEFAULT(0),
    MovementID              INT             NULL,
    CreatedAt               DATETIME2       NOT NULL    DEFAULT(GETDATE()),
    CreatedBy               INT             NULL,
    UpdatedAt               DATETIME2       NULL,
    UpdatedBy               INT             NULL
);

-- ================================================================
-- BLOQUE 3: ALTER TABLE EN TABLAS EXISTENTES
-- ----------------------------------------------------------------
-- Sin eliminar columnas existentes. Compatibilidad total con el
-- código actual en producción. Las columnas legacy (ContractText,
-- InicialContent, ContractFilepath, etc.) se mantienen como puente
-- de migración hasta que el motor genérico las reemplace.
-- ================================================================


-- ----------------------------------------------------------------
-- 3.1 HR.tbl_contract_type
-- ----------------------------------------------------------------
-- Columnas nuevas:
--   DocumentTemplateTypeID : Asocia este tipo de contrato con su
--     familia documental en ref_Types (DOCUMENT_TEMPLATE_TYPE).
--     Permite al motor saber qué plantillas usar para este tipo.
--   DefaultTemplateID      : Plantilla activa por defecto para
--     este tipo de contrato. El motor la usa cuando no se especifica
--     una versión explícita al generar.
-- ----------------------------------------------------------------
ALTER TABLE HR.tbl_contract_type
    ADD DocumentTemplateTypeID  INT NULL,
        DefaultTemplateID       INT NULL;


-- ----------------------------------------------------------------
-- 3.2 HR.tbl_Contracts
-- ----------------------------------------------------------------
-- Columnas nuevas:
--   GeneratedDocumentID  : Enlace al snapshot del documento emitido
--     para este contrato en tbl_GeneratedDocuments.
--   TemplateVersionUsed  : Versión de plantilla con la que se generó
--     el documento. Permite auditar qué formato estaba vigente.
--   IsDocumentFrozen     : Cuando es 1, el documento fue emitido y
--     no debe regenerarse aunque cambien los datos del contrato o
--     del empleado. Protege la integridad del histórico legal.
-- ----------------------------------------------------------------
ALTER TABLE HR.tbl_Contracts
    ADD GeneratedDocumentID     INT NULL,
        TemplateVersionUsed     INT NULL,
        IsDocumentFrozen        BIT NOT NULL DEFAULT(0);


-- ----------------------------------------------------------------
-- 3.3 HR.tbl_PersonnelMovements
-- ----------------------------------------------------------------
-- Columna nueva:
--   PersonnelActionID : Referencia a la acción de personal que
--     originó este movimiento. Cierra el ciclo acto → efecto.
--     Permite trazabilidad completa: documento → acción → movimiento.
-- ----------------------------------------------------------------
ALTER TABLE HR.tbl_PersonnelMovements
    ADD PersonnelActionID       INT NULL;

-- ================================================================
-- BLOQUE 4: PRIMARY KEYS
-- ================================================================

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT PK_DocumentTemplates
    PRIMARY KEY (DocumentTemplateID);

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT PK_DocumentTemplateFields
    PRIMARY KEY (DocumentTemplateFieldID);

ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT PK_GeneratedDocuments
    PRIMARY KEY (GeneratedDocumentID);

ALTER TABLE HR.tbl_GeneratedDocumentFields
    ADD CONSTRAINT PK_GeneratedDocumentFields
    PRIMARY KEY (GeneratedDocumentFieldID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT PK_PersonnelActions
    PRIMARY KEY (PersonnelActionID);

-- ================================================================
-- BLOQUE 5: UNIQUE CONSTRAINTS
-- ----------------------------------------------------------------
-- UQ_DocumentTemplates_CodeVersion:
--   Impide registrar dos veces la misma versión del mismo formato.
--   TemplateCode agrupa versiones; VersionNumber las distingue.
--
-- UQ_DocumentTemplateFields_TemplateFieldCode:
--   Un placeholder solo puede declararse una vez por plantilla.
--   Evita ambigüedad en la resolución de valores al renderizar.
--
-- UQ_PersonnelActions_ActionNumber:
--   El número oficial de acción debe ser único en el sistema.
--   Filtrado: solo aplica cuando ActionNumber no es NULL.
-- ================================================================

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT UQ_DocumentTemplates_CodeVersion
    UNIQUE (TemplateCode, VersionNumber);

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT UQ_DocumentTemplateFields_TemplateFieldCode
    UNIQUE (DocumentTemplateID, FieldCode);

CREATE UNIQUE INDEX UQ_PersonnelActions_ActionNumber
    ON HR.tbl_PersonnelActions (ActionNumber)
    WHERE ActionNumber IS NOT NULL;

-- ================================================================
-- BLOQUE 6: CHECK CONSTRAINTS
-- ================================================================

-- LayoutType: determina el renderer del backend. Solo valores conocidos.
ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT CHK_DocumentTemplates_LayoutType
    CHECK (LayoutType IN ('FLOW_TEXT', 'STRUCTURED_FORM', 'HYBRID'));

-- Status: ciclo de vida de la plantilla. Solo avance secuencial.
ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT CHK_DocumentTemplates_Status
    CHECK (Status IN ('DRAFT', 'PUBLISHED', 'ARCHIVED'));

-- VersionNumber: no puede ser menor a 1.
ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT CHK_DocumentTemplates_VersionNumber
    CHECK (VersionNumber >= 1);

-- Vigencia: EffectiveTo debe ser posterior a EffectiveFrom si se define.
ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT CHK_DocumentTemplates_EffectiveDates
    CHECK (EffectiveTo IS NULL OR EffectiveTo > EffectiveFrom);

-- GeneratedFormat: solo formatos soportados por el motor de renderizado.
ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT CHK_GeneratedDocuments_Format
    CHECK (GeneratedFormat IN ('PDF', 'HTML', 'DOCX'));

-- EntityType: entidades operativas soportadas por el motor genérico.
ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT CHK_GeneratedDocuments_EntityType
    CHECK (EntityType IN ('CONTRACT', 'PERSONNEL_ACTION', 'AGREEMENT', 'OFICIO'));

-- DataType de campos: tipos lógicos válidos para resolución de placeholders.
ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT CHK_DocumentTemplateFields_DataType
    CHECK (DataType IN ('TEXT', 'DATE', 'NUMBER', 'BOOLEAN', 'CURRENCY'));

-- SourceType: orígenes válidos para resolución automática de placeholders.
ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT CHK_DocumentTemplateFields_SourceType
    CHECK (SourceType IS NULL OR SourceType IN ('SYSTEM', 'EMPLOYEE', 'CONTRACT', 'MOVEMENT', 'MANUAL'));

-- EndDate de acción de personal debe ser posterior a EffectiveDate si se define.
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT CHK_PersonnelActions_Dates
    CHECK (EndDate IS NULL OR EndDate >= EffectiveDate);

-- ================================================================
-- BLOQUE 7: FOREIGN KEYS
-- ================================================================

-- ---- tbl_DocumentTemplates ----

-- Familia documental en ref_Types
ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT FK_DocumentTemplates_TemplateType
    FOREIGN KEY (DocumentTemplateTypeID) REFERENCES HR.ref_Types (TypeID);

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT FK_DocumentTemplates_CreatedBy
    FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

ALTER TABLE HR.tbl_DocumentTemplates
    ADD CONSTRAINT FK_DocumentTemplates_UpdatedBy
    FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees (EmployeeID);


-- ---- tbl_DocumentTemplateFields ----

-- Cada campo pertenece a una versión específica de plantilla
ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT FK_DocumentTemplateFields_Template
    FOREIGN KEY (DocumentTemplateID) REFERENCES HR.tbl_DocumentTemplates (DocumentTemplateID);

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT FK_DocumentTemplateFields_CreatedBy
    FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees (EmployeeID);

ALTER TABLE HR.tbl_DocumentTemplateFields
    ADD CONSTRAINT FK_DocumentTemplateFields_UpdatedBy
    FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees (EmployeeID);


-- ---- tbl_GeneratedDocuments ----

-- Plantilla usada al momento de emisión
ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT FK_GeneratedDocuments_Template
    FOREIGN KEY (DocumentTemplateID) REFERENCES HR.tbl_DocumentTemplates (DocumentTemplateID);

-- Archivo físico generado (PDF/DOCX)
ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT FK_GeneratedDocuments_StoredFile
    FOREIGN KEY (StoredFileID) REFERENCES HR.TBL_StoredFile (FileId);

-- Estado del documento → ref_Types (GENERATED_DOC_STATUS)
ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT FK_GeneratedDocuments_Status
    FOREIGN KEY (StatusTypeID) REFERENCES HR.ref_Types (TypeID);

ALTER TABLE HR.tbl_GeneratedDocuments
    ADD CONSTRAINT FK_GeneratedDocuments_GeneratedBy
    FOREIGN KEY (GeneratedBy) REFERENCES HR.tbl_Employees (EmployeeID);


-- ---- tbl_GeneratedDocumentFields ----

-- Cada valor de campo pertenece a un documento emitido específico
ALTER TABLE HR.tbl_GeneratedDocumentFields
    ADD CONSTRAINT FK_GeneratedDocumentFields_Document
    FOREIGN KEY (GeneratedDocumentID) REFERENCES HR.tbl_GeneratedDocuments (GeneratedDocumentID);


-- ---- tbl_PersonnelActions ----

-- Tipo de acción → ref_Types (ACTION_TYPE)
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_ActionType
    FOREIGN KEY (ActionTypeID) REFERENCES HR.ref_Types (TypeID);

-- Empleado al que se aplica la acción
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_Employee
    FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees (EmployeeID);

-- Contrato vigente al momento de la acción
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_Contract
    FOREIGN KEY (ContractID) REFERENCES HR.tbl_Contracts (ContractID);

-- Departamentos de origen y destino
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_OriginDept
    FOREIGN KEY (OriginDepartmentID) REFERENCES HR.tbl_Departments (DepartmentID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_DestDept
    FOREIGN KEY (DestDepartmentID) REFERENCES HR.tbl_Departments (DepartmentID);

-- Cargos de origen y destino
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_OriginJob
    FOREIGN KEY (OriginJobID) REFERENCES HR.tbl_jobs (JobID);

ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_DestJob
    FOREIGN KEY (DestJobID) REFERENCES HR.tbl_jobs (JobID);

-- Estado de la acción → ref_Types (PERSONNEL_ACTION_STATUS)
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_Status
    FOREIGN KEY (StatusTypeID) REFERENCES HR.ref_Types (TypeID);

-- Documento emitido asociado a esta acción
ALTER TABLE HR.tbl_PersonnelActions
    ADD CONSTRAINT FK_PersonnelActions_GeneratedDocument
    FOREIGN KEY (GeneratedDocumentID) REFERENCES HR.tbl_GeneratedDocuments (GeneratedDocumentID);

-- Movimiento producido como efecto operativo de la acción
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

-- DefaultTemplateID apunta a la plantilla activa por defecto de este tipo de contrato
ALTER TABLE HR.tbl_contract_type
    ADD CONSTRAINT FK_contract_type_DefaultTemplate
    FOREIGN KEY (DefaultTemplateID) REFERENCES HR.tbl_DocumentTemplates (DocumentTemplateID);


-- ---- tbl_Contracts (columna nueva) ----

ALTER TABLE HR.tbl_Contracts
    ADD CONSTRAINT FK_Contracts_GeneratedDocument
    FOREIGN KEY (GeneratedDocumentID) REFERENCES HR.tbl_GeneratedDocuments (GeneratedDocumentID);


-- ---- tbl_PersonnelMovements (columna nueva) ----

-- Cierre del ciclo: movimiento referencia la acción que lo originó
ALTER TABLE HR.tbl_PersonnelMovements
    ADD CONSTRAINT FK_PersonnelMovements_PersonnelAction
    FOREIGN KEY (PersonnelActionID) REFERENCES HR.tbl_PersonnelActions (PersonnelActionID);

-- ================================================================
-- BLOQUE 8: ÍNDICES DE PERFORMANCE
-- ----------------------------------------------------------------
-- Criterio de diseño:
--   Cada índice responde a una consulta real del motor o del módulo HR.
--   Se incluyen columnas INCLUDE para cubrir los SELECT más frecuentes
--   sin acceder a la tabla base (covering index).
-- ================================================================


-- ----------------------------------------------------------------
-- Plantillas: búsqueda por tipo y estado
-- Consulta: "dame las plantillas publicadas de tipo Contrato"
-- Usada por el motor al iniciar generación de documento
-- ----------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_DocumentTemplates_TypeStatus
ON HR.tbl_DocumentTemplates (DocumentTemplateTypeID, Status, IsActive)
INCLUDE (TemplateCode, VersionNumber, EffectiveFrom, EffectiveTo, LayoutType);


-- ----------------------------------------------------------------
-- Plantillas: única versión publicada activa por formato
-- Constraint de negocio crítico: un TemplateCode solo puede tener
-- una versión PUBLISHED + IsActive=1 simultáneamente.
-- El motor depende de esta unicidad para seleccionar la plantilla vigente.
-- ----------------------------------------------------------------
CREATE UNIQUE INDEX UX_DocumentTemplates_OnePublishedPerCode
ON HR.tbl_DocumentTemplates (TemplateCode)
WHERE (Status = 'PUBLISHED' AND IsActive = 1);


-- ----------------------------------------------------------------
-- Plantillas: búsqueda por vigencia en fecha de emisión
-- Consulta: "qué plantilla de CONTRATO_NOMBRAMIENTO estaba vigente el 2024-03-01"
-- Usada cuando se regenera o audita un documento histórico
-- ----------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_DocumentTemplates_CodeEffective
ON HR.tbl_DocumentTemplates (TemplateCode, EffectiveFrom, EffectiveTo)
INCLUDE (DocumentTemplateID, VersionNumber, Status, IsActive);


-- ----------------------------------------------------------------
-- Campos de plantilla: carga de todos los placeholders de una versión
-- Consulta: "dame todos los campos de la plantilla ID=5 ordenados"
-- Usada por el motor al construir el contexto de resolución
-- ----------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_DocumentTemplateFields_Template
ON HR.tbl_DocumentTemplateFields (DocumentTemplateID, SortOrder)
INCLUDE (FieldCode, FieldLabel, DataType, IsRequired, SourceType, SourcePath, FormatPattern);


-- ----------------------------------------------------------------
-- Documentos generados: historial por entidad operativa
-- Consulta: "dame todos los documentos emitidos del contrato ID=42"
-- Usada en la vista de historial documental del empleado/contrato
-- ----------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_GeneratedDocuments_Entity
ON HR.tbl_GeneratedDocuments (EntityType, EntityID, GeneratedAt DESC)
INCLUDE (DocumentTemplateID, TemplateVersionNumber, DocumentNumber, GeneratedFormat, StatusTypeID, StoredFileID);


-- ----------------------------------------------------------------
-- Documentos generados: búsqueda por número de documento oficial
-- Consulta: "buscar resolución número RES-2024-0042"
-- Filtrado: solo cuando DocumentNumber no es NULL (documentos oficiales)
-- ----------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_GeneratedDocuments_DocumentNumber
ON HR.tbl_GeneratedDocuments (DocumentNumber)
WHERE DocumentNumber IS NOT NULL;


-- ----------------------------------------------------------------
-- Campos de documento generado: reconstrucción de valores por documento
-- Consulta: "dame todos los valores de placeholders del documento emitido ID=100"
-- Consulta más frecuente en auditoría y vista de histórico
-- ----------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_GeneratedDocumentFields_Document
ON HR.tbl_GeneratedDocumentFields (GeneratedDocumentID)
INCLUDE (FieldCode, RawValue, RenderedValue);


-- ----------------------------------------------------------------
-- Acciones de personal: por empleado y estado
-- Consulta: "dame todas las acciones activas del empleado ID=15"
-- Usada en el módulo de RRHH para listado de trámites del empleado
-- ----------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_PersonnelActions_EmployeeStatus
ON HR.tbl_PersonnelActions (EmployeeID, StatusTypeID, EffectiveDate DESC)
INCLUDE (ActionTypeID, ContractID, ActionNumber, IsDocumentFrozen, GeneratedDocumentID);


-- ----------------------------------------------------------------
-- Acciones de personal: por contrato
-- Consulta: "dame todas las acciones del contrato ID=10"
-- Usada al abrir un contrato para ver acciones relacionadas
-- ----------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_PersonnelActions_Contract
ON HR.tbl_PersonnelActions (ContractID, EffectiveDate DESC)
INCLUDE (PersonnelActionID, ActionTypeID, StatusTypeID, ActionNumber);
-- ================================================================
-- BLOQUE 9: SEED ACTION_TYPE (requerido por tbl_PersonnelActions.ActionTypeID)
-- ----------------------------------------------------------------
-- Tipos de acción de personal según el formulario institucional
-- de la Universidad Técnica de Ambato (LOSEP / RLOSEP art. 21)
-- ================================================================
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, SortOrder, CreatedBy)
VALUES
('ACTION_TYPE', 'Ingreso',                  'Ingreso de nuevo servidor público',                          1, 10, NULL),
('ACTION_TYPE', 'Reingreso',                'Reingreso de servidor que estuvo separado',                  1, 20, NULL),
('ACTION_TYPE', 'Restitución',              'Restitución al cargo por resolución o sentencia',            1, 30, NULL),
('ACTION_TYPE', 'Reintegro',                'Reintegro luego de licencia o comisión',                     1, 40, NULL),
('ACTION_TYPE', 'Ascenso',                  'Cambio a cargo de mayor jerarquía',                          1, 50, NULL),
('ACTION_TYPE', 'Traslado',                 'Cambio de dependencia del servidor',                         1, 60, NULL),
('ACTION_TYPE', 'Traspaso',                 'Transferencia entre instituciones',                          1, 70, NULL),
('ACTION_TYPE', 'Cambio Administrativo',    'Cambio de unidad dentro de la misma institución',            1, 80, NULL),
('ACTION_TYPE', 'Intercambio Voluntario',   'Intercambio de puestos entre servidores',                    1, 90, NULL),
('ACTION_TYPE', 'Licencia',                 'Ausencia autorizada con o sin remuneración',                 1, 100, NULL),
('ACTION_TYPE', 'Comisión de Servicios',    'Prestación temporal de servicios en otra dependencia',       1, 110, NULL),
('ACTION_TYPE', 'Sanciones',                'Registro de sanciones disciplinarias',                       1, 120, NULL),
('ACTION_TYPE', 'Incremento RMU',           'Incremento de remuneración mensual unificada',               1, 130, NULL),
('ACTION_TYPE', 'Subrogación',              'Asunción temporal de funciones de cargo superior',           1, 140, NULL),
('ACTION_TYPE', 'Encargo',                  'Encargo de funciones de otro cargo',                         1, 150, NULL),
('ACTION_TYPE', 'Cesación de Funciones',    'Terminación de la relación laboral',                         1, 160, NULL),
('ACTION_TYPE', 'Destitución',              'Separación forzosa del servicio',                            1, 170, NULL),
('ACTION_TYPE', 'Vacaciones',               'Goce de vacaciones anuales',                                 1, 180, NULL),
('ACTION_TYPE', 'Revisión Clasif. Puesto',  'Revisión y reclasificación del puesto',                      1, 190, NULL),
('ACTION_TYPE', 'Encargo del Puesto',       'Encargo específico del puesto institucional',                 1, 200, NULL);
