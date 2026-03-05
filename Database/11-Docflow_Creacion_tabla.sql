/* ============================================================
   DOCFLOW - CREACIÓN DESDE CERO (MEJORADO)
   BLOQUE 1/3: CREACIÓN DE TABLAS (SOLO CREATE TABLE)
   - Sin PK
   - Sin FK
   - Sin CHECK/DEFAULT constraints
   - Solo definiciones de columnas + descripciones (comentarios)
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'docflow')
    EXEC('CREATE SCHEMA docflow');
GO

/* ------------------------------------------------------------
   tbl_Users: usuarios del sistema Docflow
   ------------------------------------------------------------ */
-- CREATE TABLE docflow.tbl_Users
-- (
    -- UserId      INT IDENTITY(1,1) NOT NULL,      -- Identificador interno del usuario
    -- Username    NVARCHAR(100) NOT NULL,          -- Usuario (login) - debe ser único (constraint en bloque 2)
    -- FullName    NVARCHAR(200) NOT NULL,          -- Nombre completo para auditoría/UI
    -- Email       NVARCHAR(200) NULL,              -- Correo (opcional)
    -- IsActive    BIT NOT NULL,                    -- Activo/inactivo
    -- CreatedAt   DATETIME2 NOT NULL,              -- Fecha creación
    -- UpdatedAt   DATETIME2 NULL                   -- Fecha actualización
-- );
-- GO

/* ------------------------------------------------------------
   tbl_ProcessHierarchy: procesos/etapas (jerarquía + depto responsable)
   ------------------------------------------------------------ */
CREATE TABLE docflow.tbl_ProcessHierarchy
(
    ProcessId   INT IDENTITY(1,1) NOT NULL,      -- Identificador del proceso/etapa
    ParentId    INT NULL,                        -- Proceso padre (para jerarquía)
    ProcessCode NVARCHAR(50) NOT NULL,           -- Código único del proceso
    ProcessName NVARCHAR(200) NOT NULL,          -- Nombre del proceso/etapa

    ResponsibleDepartmentId INT NOT NULL,        -- Depto responsable de tramitar esta etapa (según CurrentUserService)
	ProcessFolderName	NVARCHAR(100) NULL,		 -- Nombre de la carpeta del proceso en donde estara todos los expedientes y los archivos 
	
	DynamicFieldMetadata NVARCHAR(MAX) NULL,	 -- Contiene todos los metadatos con sus valores para cada proceso y/o subproceso

    IsActive    BIT NOT NULL,                    -- Activo/inactivo
    CreatedBy   INT NULL,                        -- Usuario creador (FK en bloque 2)	
    CreatedAt   DATETIME2 NOT NULL,              -- Fecha creación
    UpdatedBy   INT NULL,                        -- Usuario actualiza (FK en bloque 2)
    UpdatedAt   DATETIME2 NULL                   -- Fecha actualización
);
GO

/* ------------------------------------------------------------
   tbl_ProcessTransitions: secuencia real del workflow
   ------------------------------------------------------------ */
CREATE TABLE docflow.tbl_ProcessTransitions
(
    TransitionId INT IDENTITY(1,1) NOT NULL,     -- Identificador de transición
    FromProcessId INT NOT NULL,                  -- Proceso actual
    ToProcessId   INT NOT NULL,                  -- Proceso siguiente

    IsDefault   BIT NOT NULL,                    -- Ruta por defecto si hay múltiples salidas
    AllowReturn BIT NOT NULL,                    -- Permite retorno desde este paso
    ReturnToProcessId INT NULL,                  -- Proceso destino de retorno (si no es al paso anterior por historial)

    CreatedBy INT NULL,                          -- Usuario creador (FK en bloque 2)
    CreatedAt DATETIME2 NOT NULL,                -- Fecha creación
    UpdatedBy INT NULL,                          -- Usuario actualiza (FK en bloque 2)
    UpdatedAt DATETIME2 NULL                     -- Fecha actualización
);
GO

/* ------------------------------------------------------------
   tbl_DocumentRules: requisitos por proceso
   ------------------------------------------------------------ */
CREATE TABLE docflow.tbl_DocumentRules
(
    RuleId      INT IDENTITY(1,1) NOT NULL,      -- Identificador de requisito/regla
    ProcessId   INT NOT NULL,                    -- Proceso al que aplica el requisito (FK en bloque 2)
    DocumentType NVARCHAR(100) NOT NULL,         -- Tipo/clase de documento (clave funcional)
    IsRequired  BIT NOT NULL,                    -- Requisito obligatorio o no

    DefaultVisibility TINYINT NOT NULL,          -- 1=PUBLIC_WITHIN_CASE, 2=PRIVATE_TO_UPLOADER_DEPT (CHECK en bloque 2)
    AllowVisibilityOverride BIT NOT NULL,         -- Permite override al cargar (según roles/reglas)

    CreatedBy   INT NULL,
    CreatedAt   DATETIME2 NOT NULL,
    UpdatedBy   INT NULL,
    UpdatedAt   DATETIME2 NULL
);
GO

/* ------------------------------------------------------------
   tbl_WorkflowInstances: expedientes (proceso actual + depto actual)
   ------------------------------------------------------------ */
CREATE TABLE docflow.tbl_WorkflowInstances
(
    InstanceId  UNIQUEIDENTIFIER NOT NULL,       -- Identificador del expediente
	RootProcessId INT NULL,        				 -- ID del macroproceso raíz
	InstanceName NVARCHAR(255) NULL; 			 -- Nombre del expediente
    ProcessId   INT NOT NULL,                    -- Proceso/etapa actual (FK en bloque 2)
    CurrentStatus NVARCHAR(50) NOT NULL,         -- Estado funcional del expediente (CHECK básico en bloque 2)

    CurrentDepartmentId INT NOT NULL,            -- Depto actual responsable (derivado del proceso actual)

    AssignedToUserId INT NULL,                   -- Usuario asignado (opcional)
    DynamicMetadata NVARCHAR(MAX) NULL,           -- JSON: metadata dinámica del expediente

    CreatedBy   INT NULL,
    CreatedAt   DATETIME2 NOT NULL,
    UpdatedBy   INT NULL,
    UpdatedAt   DATETIME2 NULL
);
GO

/* ------------------------------------------------------------
   tbl_Documents: documentos lógicos del expediente (visibilidad + depto creador)
   ------------------------------------------------------------ */
CREATE TABLE docflow.tbl_Documents
(
    DocumentId  UNIQUEIDENTIFIER NOT NULL,       -- Identificador del documento lógico
    InstanceId  UNIQUEIDENTIFIER NOT NULL,       -- Expediente al que pertenece (FK en bloque 2)
    RuleId      INT NULL,                        -- Requisito asociado (opcional) (FK en bloque 2)
    DocumentName NVARCHAR(255) NOT NULL,         -- Nombre funcional

    CreatedByDepartmentId INT NOT NULL,          -- Depto del creador/cargador del documento

    Visibility TINYINT NOT NULL,                 -- 1=PUBLIC_WITHIN_CASE, 2=PRIVATE_TO_UPLOADER_DEPT (CHECK en bloque 2)

    CurrentVersion INT NOT NULL,                 -- Última versión (número)
    IsDeleted  BIT NOT NULL,                     -- Soft delete

    CreatedBy   INT NULL,
    CreatedAt   DATETIME2 NOT NULL,
    UpdatedBy   INT NULL,
    UpdatedAt   DATETIME2 NULL
);
GO

/* ------------------------------------------------------------
   tbl_FileVersions: versiones físicas (storage)
   ------------------------------------------------------------ */
CREATE TABLE docflow.tbl_FileVersions
(
    VersionId   UNIQUEIDENTIFIER NOT NULL,       -- Identificador de la versión
    DocumentId  UNIQUEIDENTIFIER NOT NULL,       -- Documento lógico padre (FK en bloque 2)
    VersionNumber INT NOT NULL,                  -- Número de versión incremental (1..n)

    StoragePath NVARCHAR(1000) NOT NULL,         -- Ruta/clave interna del storage (nunca exponer al frontend)
    FileExtension NVARCHAR(20) NULL,             -- Extensión (pdf, docx, etc.)
    FileSizeInBytes BIGINT NULL,                 -- Tamaño en bytes
    ChecksumHash NVARCHAR(128) NULL,             -- Hash de integridad/dedupe (opcional)

    CreatedBy   INT NULL,
    CreatedAt   DATETIME2 NOT NULL
);
GO

/* ------------------------------------------------------------
   tbl_WorkflowMovements: movimientos (FORWARD/RETURN) + trazabilidad
   ------------------------------------------------------------ */
CREATE TABLE docflow.tbl_WorkflowMovements
(
    MovementId  UNIQUEIDENTIFIER NOT NULL,       -- Identificador del movimiento
    InstanceId  UNIQUEIDENTIFIER NOT NULL,       -- Expediente asociado (FK en bloque 2)

    MovementType NVARCHAR(10) NOT NULL,          -- 'FORWARD' / 'RETURN' (CHECK en bloque 2)
    Comments    NVARCHAR(2000) NULL,             -- Motivo/observación (recomendado obligatorio en RETURN)

    AssignedToUserId INT NULL,                   -- (Opcional) reasignación

    FromProcessId INT NULL,                      -- Proceso origen
    ToProcessId   INT NULL,                      -- Proceso destino
    FromDepartmentId INT NULL,                   -- Depto origen
    ToDepartmentId   INT NULL,                   -- Depto destino

    CreatedBy   INT NULL,
    CreatedAt   DATETIME2 NOT NULL
);
GO
