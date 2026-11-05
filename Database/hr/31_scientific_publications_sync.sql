/* ============================================================================
   31_scientific_publications_sync.sql
   EJECUTAR CON UTF-8 (sqlcmd -f 65001): el script tiene tildes; con la página de códigos por defecto no encuentra la carpeta "Investigación" existente y crea un menú duplicado con texto dañado.

   Sincronización de publicaciones científicas (Scopus y otros proveedores) con revisión y aprobación.

   - Catálogo de proveedores: HR.ref_Types, categoría SCIENTIFIC_PROVIDER (Name = código estable, Description = texto,
     Metadata = configuración JSON: etiqueta del ID, patrón, enlace al perfil, requiresProviderId, allowsMultipleIds,
     requiresApproval, system, usesProfileOrcid). Qué proveedores tienen adaptador de sincronización lo decide el código.
   - HR.tbl_ResearcherProfiles: perfil de investigador por persona (ORCID obligatorio para sincronizar; único).
   - HR.tbl_ResearcherProviderIds: ID del investigador en cada plataforma (N a 1 con el perfil).
   - HR.tbl_Publications: origen (SourceProviderTypeID), ID externo, DOI normalizado, estado de revisión
     (SIN_REVISION, APROBADA, RECHAZADA) y fecha de sincronización. Las filas existentes pasan a MIGRACION.
   - Permiso PUBLICATIONS.APPROVE (R_PROYECTOS y Administrador) y opción de menú "Revisión de Publicaciones".
   - Parámetros: URL del servicio y credenciales del proveedor Scopus (las credenciales quedan vacías; se cargan después).
   Idempotente. Esquemas HR y auth. No limpia ni modifica datos existentes salvo asignar origen/estado a las publicaciones.
   ============================================================================ */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

BEGIN TRAN;

-- 1) Catálogo de proveedores (ref_Types) ---------------------------------------
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, SortOrder, Metadata, CreatedAt)
SELECT 'SCIENTIFIC_PROVIDER', v.Name, v.Description, 1, v.SortOrder, v.Metadata, SYSDATETIME()
FROM (VALUES
    (N'SCOPUS',    N'Elsevier (Scopus)',                 1,
     N'{"label":"Scopus Author ID","idPattern":"^[0-9]{6,16}$","profileUrl":"https://www.scopus.com/authid/detail.uri?authorId={id}","requiresProviderId":false,"allowsMultipleIds":true,"usesProfileOrcid":false,"requiresApproval":true,"system":false}'),
    (N'ORCID',     N'ORCID',                             2,
     N'{"label":"ORCID iD","profileUrl":"https://orcid.org/{id}","requiresProviderId":false,"allowsMultipleIds":false,"usesProfileOrcid":true,"requiresApproval":true,"system":false}'),
    (N'WOS',       N'Web of Science',                    3,
     N'{"label":"Web of Science ResearcherID","requiresProviderId":true,"allowsMultipleIds":true,"usesProfileOrcid":false,"requiresApproval":true,"system":false}'),
    (N'MANUAL',    N'Registro manual',                  90,
     N'{"system":true,"requiresApproval":false}'),
    (N'MIGRACION', N'Migración histórica (UTA Mático)', 91,
     N'{"system":true,"requiresApproval":false}')
) AS v(Name, Description, SortOrder, Metadata)
WHERE NOT EXISTS (SELECT 1 FROM HR.ref_Types t WHERE t.Category = 'SCIENTIFIC_PROVIDER' AND t.Name = v.Name);

-- 2) Perfil de investigador (1 por persona; ORCID único) --------------------------
IF OBJECT_ID('HR.tbl_ResearcherProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_ResearcherProfiles (
        ResearcherProfileID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearcherProfiles PRIMARY KEY,
        PersonID            INT NOT NULL,
        Orcid               VARCHAR(19) NOT NULL,
        OrcidEnteredAt      DATETIME2 NOT NULL CONSTRAINT DF_ResearcherProfiles_EnteredAt DEFAULT (SYSDATETIME()),
        OrcidEnteredBy      INT NULL,
        CreatedAt           DATETIME2 NOT NULL CONSTRAINT DF_ResearcherProfiles_CreatedAt DEFAULT (SYSDATETIME()),
        CreatedBy           INT NULL,
        UpdatedAt           DATETIME2 NULL,
        UpdatedBy           INT NULL,
        CONSTRAINT UQ_ResearcherProfiles_Person UNIQUE (PersonID),
        CONSTRAINT UQ_ResearcherProfiles_Orcid UNIQUE (Orcid),
        CONSTRAINT FK_ResearcherProfiles_Person FOREIGN KEY (PersonID) REFERENCES HR.tbl_People (PersonID),
        -- BIN: la X del dígito verificador va en mayúscula (la intercalación de la BD no distingue mayúsculas)
        CONSTRAINT CK_ResearcherProfiles_Orcid CHECK (
            Orcid COLLATE Latin1_General_BIN LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9X]')
    );
END

-- 3) IDs del investigador por plataforma (N a 1 con el perfil) -------------------
IF OBJECT_ID('HR.tbl_ResearcherProviderIds', 'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_ResearcherProviderIds (
        ResearcherProviderIdID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearcherProviderIds PRIMARY KEY,
        ResearcherProfileID    INT NOT NULL,
        ProviderTypeID         INT NOT NULL,
        ExternalId             NVARCHAR(50) NOT NULL,
        IsActive               BIT NOT NULL CONSTRAINT DF_ResearcherProviderIds_IsActive DEFAULT (1),
        VerifiedAt             DATETIME2 NULL,
        LastSyncedAt           DATETIME2 NULL,
        LastSyncStatus         NVARCHAR(12) NULL,
        LastSyncSummary        NVARCHAR(500) NULL,
        CreatedAt              DATETIME2 NOT NULL CONSTRAINT DF_ResearcherProviderIds_CreatedAt DEFAULT (SYSDATETIME()),
        CreatedBy              INT NULL,
        UpdatedAt              DATETIME2 NULL,
        UpdatedBy              INT NULL,
        CONSTRAINT FK_ResearcherProviderIds_Profile  FOREIGN KEY (ResearcherProfileID) REFERENCES HR.tbl_ResearcherProfiles (ResearcherProfileID) ON DELETE CASCADE,
        CONSTRAINT FK_ResearcherProviderIds_Provider FOREIGN KEY (ProviderTypeID) REFERENCES HR.ref_Types (TypeID),
        CONSTRAINT CK_ResearcherProviderIds_Status CHECK (LastSyncStatus IS NULL OR LastSyncStatus IN ('OK', 'PARCIAL', 'ERROR'))
    );
    -- Una persona no repite el mismo ID del mismo proveedor...
    CREATE UNIQUE INDEX UX_ResearcherProviderIds_Profile_Provider_Id
        ON HR.tbl_ResearcherProviderIds (ResearcherProfileID, ProviderTypeID, ExternalId);
    -- ...y un ID activo de un proveedor no puede pertenecer a dos personas.
    CREATE UNIQUE INDEX UX_ResearcherProviderIds_Provider_Id_Active
        ON HR.tbl_ResearcherProviderIds (ProviderTypeID, ExternalId) WHERE IsActive = 1;
END

-- 4) Publicaciones: origen, ID externo, DOI normalizado y estado de revisión ------
DECLARE @ManualId INT = (SELECT TypeID FROM HR.ref_Types WHERE Category = 'SCIENTIFIC_PROVIDER' AND Name = 'MANUAL');
DECLARE @MigracionId INT = (SELECT TypeID FROM HR.ref_Types WHERE Category = 'SCIENTIFIC_PROVIDER' AND Name = 'MIGRACION');
IF @ManualId IS NULL OR @MigracionId IS NULL
    THROW 51000, 'Faltan los proveedores MANUAL/MIGRACION en ref_Types (categoría SCIENTIFIC_PROVIDER).', 1;

IF COL_LENGTH('HR.tbl_Publications', 'SourceProviderTypeID') IS NULL
BEGIN
    -- Por defecto MANUAL: lo que registra el código actual sin conocer la columna.
    DECLARE @sql NVARCHAR(MAX) = N'ALTER TABLE HR.tbl_Publications ADD SourceProviderTypeID INT NOT NULL
           CONSTRAINT DF_Publications_SourceProvider DEFAULT (' + CAST(@ManualId AS NVARCHAR(10)) + N')';
    EXEC(@sql);
    -- Todo lo que ya existía es carga histórica (solo al crear la columna: no pisa registros MANUAL posteriores).
    SET @sql = N'UPDATE HR.tbl_Publications SET SourceProviderTypeID = ' + CAST(@MigracionId AS NVARCHAR(10));
    EXEC(@sql);
END

IF COL_LENGTH('HR.tbl_Publications', 'ExternalId') IS NULL
    ALTER TABLE HR.tbl_Publications ADD ExternalId NVARCHAR(60) NULL;

IF COL_LENGTH('HR.tbl_Publications', 'ApprovalStatus') IS NULL
    ALTER TABLE HR.tbl_Publications ADD ApprovalStatus NVARCHAR(12) NOT NULL
        CONSTRAINT DF_Publications_ApprovalStatus DEFAULT ('SIN_REVISION');

IF COL_LENGTH('HR.tbl_Publications', 'ApprovedAt') IS NULL
    ALTER TABLE HR.tbl_Publications ADD ApprovedAt DATETIME2 NULL;

IF COL_LENGTH('HR.tbl_Publications', 'ApprovedBy') IS NULL
    ALTER TABLE HR.tbl_Publications ADD ApprovedBy INT NULL;

IF COL_LENGTH('HR.tbl_Publications', 'ReviewNotes') IS NULL
    ALTER TABLE HR.tbl_Publications ADD ReviewNotes NVARCHAR(300) NULL;

IF COL_LENGTH('HR.tbl_Publications', 'LastSyncedAt') IS NULL
    ALTER TABLE HR.tbl_Publications ADD LastSyncedAt DATETIME2 NULL;

-- DOI normalizado: minúsculas, sin prefijo de URL ni "doi:"; si no empieza con "10." (p. ej. una URL de ORCID) queda NULL.
-- No es único: varias personas comparten artículo y un mismo DOI puede usarse en varios capítulos.
IF COL_LENGTH('HR.tbl_Publications', 'DoiNormalized') IS NULL
BEGIN
    DECLARE @d NVARCHAR(MAX) = N'LOWER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Doi], ''https://doi.org/'', ''''), ''http://doi.org/'', ''''), ''https://dx.doi.org/'', ''''), ''http://dx.doi.org/'', ''''), ''doi.org/'', ''''), ''doi:'', ''''))))';
    DECLARE @sqlDoi NVARCHAR(MAX) = N'ALTER TABLE HR.tbl_Publications ADD DoiNormalized AS CAST(CASE WHEN ' + @d + N' LIKE ''10.%'' THEN ' + @d + N' END AS NVARCHAR(255)) PERSISTED';
    EXEC(@sqlDoi);
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Publications_SourceProvider')
    EXEC(N'ALTER TABLE HR.tbl_Publications ADD CONSTRAINT FK_Publications_SourceProvider
           FOREIGN KEY (SourceProviderTypeID) REFERENCES HR.ref_Types (TypeID)');

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Publications_ApprovalStatus')
    EXEC(N'ALTER TABLE HR.tbl_Publications ADD CONSTRAINT CK_Publications_ApprovalStatus
           CHECK (ApprovalStatus IN (''SIN_REVISION'', ''APROBADA'', ''RECHAZADA'')
                  AND (ApprovalStatus <> ''APROBADA'' OR ApprovedAt IS NOT NULL))');

-- Evita duplicados al sincronizar (misma persona, mismo proveedor, mismo ID del artículo).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Publications_Person_Provider_ExternalId' AND object_id = OBJECT_ID('HR.tbl_Publications'))
    EXEC(N'CREATE UNIQUE INDEX UX_Publications_Person_Provider_ExternalId
           ON HR.tbl_Publications (PersonID, SourceProviderTypeID, ExternalId) WHERE ExternalId IS NOT NULL');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Publications_Person_DoiNormalized' AND object_id = OBJECT_ID('HR.tbl_Publications'))
    EXEC(N'CREATE INDEX IX_Publications_Person_DoiNormalized
           ON HR.tbl_Publications (PersonID, DoiNormalized)');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Publications_ApprovalStatus' AND object_id = OBJECT_ID('HR.tbl_Publications'))
    EXEC(N'CREATE INDEX IX_Publications_ApprovalStatus
           ON HR.tbl_Publications (ApprovalStatus, SourceProviderTypeID, PersonID)');

-- 5) Parámetros del proveedor Scopus ---------------------------------------------
-- La URL solo se acepta si su host está en la lista del adaptador (el token se envía a esa URL).
-- Las credenciales se cargan desde la pantalla de Parámetros (quedan vacías en este script).
INSERT INTO HR.tbl_Parameters ([name], Pvalues, Description, DataType, IsActive, CreatedAt)
SELECT v.Name, v.Value, v.Description, 'TEXTO', 1, SYSDATETIME()
FROM (VALUES
    ('PUBSYNC_SCOPUS_API_URL',    N'https://api.elsevier.com', N'URL base del servicio Elsevier (Scopus) para sincronizar publicaciones. Solo se acepta api.elsevier.com por HTTPS.'),
    ('PUBSYNC_SCOPUS_API_KEY',    N'',                         N'API Key de Elsevier (Scopus). Se envía en el header X-ELS-APIKey; no se muestra en pantalla.'),
    ('PUBSYNC_SCOPUS_INST_TOKEN', N'',                         N'Institutional Token de Elsevier (opcional). Se envía en el header X-ELS-Insttoken; no se muestra en pantalla.')
) AS v(Name, Value, Description)
WHERE NOT EXISTS (SELECT 1 FROM HR.tbl_Parameters p WHERE p.[name] = v.Name);

COMMIT;
-- Scopus consulta por ORCID: el Scopus Author ID es opcional (afina la búsqueda). Corrige instalaciones con el valor anterior.
UPDATE HR.ref_Types SET Metadata = REPLACE(Metadata, '"requiresProviderId":true', '"requiresProviderId":false')
WHERE Category = 'SCIENTIFIC_PROVIDER' AND Name = 'SCOPUS' AND Metadata LIKE '%"requiresProviderId":true%';
GO

-- 6) Permiso de aprobación y menú (esquema auth, compartido con RepositoryUta) -------------
INSERT INTO auth.tbl_Permissions (Name, Module, Action, Description, Version, IsDeleted)
SELECT 'PUBLICATIONS.APPROVE', 'PUBLICATIONS', 'APPROVE',
       N'Revisar, aprobar, rechazar y reabrir publicaciones sincronizadas (Dirección de Investigación)', 1, 0
WHERE NOT EXISTS (SELECT 1 FROM auth.tbl_Permissions p WHERE p.Name = 'PUBLICATIONS.APPROVE' AND p.IsDeleted = 0);

INSERT INTO auth.tbl_RolePermissions (RoleId, PermissionId, GrantedAt)
SELECT r.Id, p.Id, SYSDATETIME()
FROM auth.tbl_Roles r
JOIN auth.tbl_Permissions p ON p.Name = 'PUBLICATIONS.APPROVE' AND p.IsDeleted = 0
WHERE r.Name IN ('R_PROYECTOS', 'Administrador') AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);
GO

DECLARE @RootId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Name = N'Investigación' AND ParentId IS NULL AND IsDeleted = 0);
IF @RootId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    VALUES (N'Investigación', NULL, 'BookOpen', NULL, 4, 1, 'Investigacion', 0);
    SET @RootId = SCOPE_IDENTITY();
END

DECLARE @MenuId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Url = '/publication-review' AND IsDeleted = 0);
IF @MenuId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    VALUES (N'Revisión de Publicaciones', '/publication-review', 'ClipboardCheck', @RootId, 2, 1, 'Investigacion', 0);
    SET @MenuId = SCOPE_IDENTITY();
END

INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
SELECT r.Id, m.Id, 1
FROM auth.tbl_Roles r
CROSS JOIN (SELECT @RootId AS Id UNION ALL SELECT @MenuId) m
WHERE r.Name IN ('Administrador', 'R_PROYECTOS') AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems rm WHERE rm.RoleId = r.Id AND rm.MenuItemId = m.Id);
GO
