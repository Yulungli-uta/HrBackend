-- ============================================================================
-- 28_research_projects.sql
-- Módulo "Proyectos de Investigación": proyectos con sus resoluciones (aprobación,
-- prórroga y finalización; el archivo PDF de cada una es opcional, el número es obligatorio) y los participantes
-- (internos: persona de HR.tbl_People; externos: datos ingresados a mano).
-- Alimenta la pestaña de la hoja de vida y el bloque researchProjectParticipations del
-- servicio de escalafón docente (academic-promotion/teachers).
-- Incluye: tablas, catálogo de roles en ref_Types, rol R_PROYECTOS, permisos RESEARCH_PROJECTS.*
-- y el módulo de menú propio "Investigación" con la opción "Proyectos de Investigación". Idempotente. Esquemas HR y auth.
-- ============================================================================

-- 1) Proyectos ---------------------------------------------------------------
IF OBJECT_ID('HR.tbl_ResearchProjects', 'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_ResearchProjects (
        ResearchProjectID          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchProjects PRIMARY KEY,
        Name                       NVARCHAR(500) NOT NULL,
        StartDate                  DATE NOT NULL,
        EndDate                    DATE NOT NULL,
        ApprovalResolutionNumber   NVARCHAR(100) NOT NULL,
        ApprovalFileId             INT NULL,
        HasExtension               BIT NOT NULL CONSTRAINT DF_ResearchProjects_HasExt DEFAULT (0),
        ExtensionResolutionNumber  NVARCHAR(100) NULL,
        ExtensionEndDate           DATE NULL,
        ExtensionFileId            INT NULL,
        CompletionResolutionNumber NVARCHAR(100) NOT NULL,
        CompletionFileId           INT NULL,
        IsDeleted                  BIT NOT NULL CONSTRAINT DF_ResearchProjects_IsDeleted DEFAULT (0),
        CreatedAt                  DATETIME2 NULL,
        CreatedBy                  INT NULL,
        UpdatedAt                  DATETIME2 NULL,
        UpdatedBy                  INT NULL,
        CONSTRAINT CK_ResearchProjects_Dates CHECK (EndDate >= StartDate),
        CONSTRAINT CK_ResearchProjects_Extension CHECK (
            (HasExtension = 0 AND ExtensionResolutionNumber IS NULL AND ExtensionEndDate IS NULL AND ExtensionFileId IS NULL)
            OR (HasExtension = 1 AND ExtensionResolutionNumber IS NOT NULL AND ExtensionEndDate IS NOT NULL
                AND ExtensionEndDate > EndDate)),
        CONSTRAINT FK_ResearchProjects_ApprovalFile   FOREIGN KEY (ApprovalFileId)   REFERENCES HR.TBL_StoredFile (FileId),
        CONSTRAINT FK_ResearchProjects_ExtensionFile  FOREIGN KEY (ExtensionFileId)  REFERENCES HR.TBL_StoredFile (FileId),
        CONSTRAINT FK_ResearchProjects_CompletionFile FOREIGN KEY (CompletionFileId) REFERENCES HR.TBL_StoredFile (FileId)
    );
    CREATE INDEX IX_ResearchProjects_Dates ON HR.tbl_ResearchProjects (StartDate, EndDate) WHERE IsDeleted = 0;
END
GO

-- 1b) Los archivos de las resoluciones son opcionales (el número de cada resolución sí es obligatorio).
-- Migración para tablas creadas con archivos obligatorios. Idempotente.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HR.tbl_ResearchProjects') AND name = 'ApprovalFileId' AND is_nullable = 0)
BEGIN
    ALTER TABLE HR.tbl_ResearchProjects DROP CONSTRAINT CK_ResearchProjects_Extension;
    ALTER TABLE HR.tbl_ResearchProjects ALTER COLUMN ApprovalFileId INT NULL;
    ALTER TABLE HR.tbl_ResearchProjects ALTER COLUMN CompletionFileId INT NULL;
    ALTER TABLE HR.tbl_ResearchProjects ADD CONSTRAINT CK_ResearchProjects_Extension CHECK (
        (HasExtension = 0 AND ExtensionResolutionNumber IS NULL AND ExtensionEndDate IS NULL AND ExtensionFileId IS NULL)
        OR (HasExtension = 1 AND ExtensionResolutionNumber IS NOT NULL AND ExtensionEndDate IS NOT NULL AND ExtensionEndDate > EndDate));
END
GO

-- 2) Participaciones ---------------------------------------------------------
IF OBJECT_ID('HR.tbl_ResearchProjectParticipations', 'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_ResearchProjectParticipations (
        ParticipationID         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchProjectParticipations PRIMARY KEY,
        ResearchProjectID       INT NOT NULL,
        IsExternal              BIT NOT NULL CONSTRAINT DF_ResearchPart_IsExternal DEFAULT (0),
        PersonID                INT NULL,
        ExternalFullName        NVARCHAR(300) NULL,
        ExternalIdentification  NVARCHAR(30) NULL,
        ExternalInstitution     NVARCHAR(300) NULL,
        RoleTypeID              INT NOT NULL,
        StartDate               DATE NOT NULL,
        EndDate                 DATE NOT NULL,
        IsDeleted               BIT NOT NULL CONSTRAINT DF_ResearchPart_IsDeleted DEFAULT (0),
        CreatedAt               DATETIME2 NULL,
        CreatedBy               INT NULL,
        UpdatedAt               DATETIME2 NULL,
        UpdatedBy               INT NULL,
        CONSTRAINT CK_ResearchPart_Dates CHECK (EndDate >= StartDate),
        CONSTRAINT CK_ResearchPart_Kind CHECK (
            (IsExternal = 0 AND PersonID IS NOT NULL AND ExternalFullName IS NULL AND ExternalIdentification IS NULL)
            OR (IsExternal = 1 AND PersonID IS NULL AND ExternalFullName IS NOT NULL AND ExternalIdentification IS NOT NULL)),
        CONSTRAINT FK_ResearchPart_Project FOREIGN KEY (ResearchProjectID) REFERENCES HR.tbl_ResearchProjects (ResearchProjectID),
        CONSTRAINT FK_ResearchPart_Person  FOREIGN KEY (PersonID)          REFERENCES HR.tbl_People (PersonID),
        CONSTRAINT FK_ResearchPart_Role    FOREIGN KEY (RoleTypeID)        REFERENCES HR.ref_Types (TypeID)
    );
    CREATE INDEX IX_ResearchPart_Project ON HR.tbl_ResearchProjectParticipations (ResearchProjectID) WHERE IsDeleted = 0;
    CREATE INDEX IX_ResearchPart_Person  ON HR.tbl_ResearchProjectParticipations (PersonID) WHERE IsDeleted = 0 AND PersonID IS NOT NULL;
    -- Sin participaciones duplicadas (misma persona, rol y fecha de inicio en el mismo proyecto).
    CREATE UNIQUE INDEX UX_ResearchPart_Internal ON HR.tbl_ResearchProjectParticipations (ResearchProjectID, PersonID, RoleTypeID, StartDate)
        WHERE IsDeleted = 0 AND PersonID IS NOT NULL;
    CREATE UNIQUE INDEX UX_ResearchPart_External ON HR.tbl_ResearchProjectParticipations (ResearchProjectID, ExternalIdentification, RoleTypeID, StartDate)
        WHERE IsDeleted = 0 AND ExternalIdentification IS NOT NULL;
END
GO

-- 2b) Se quita la etapa de la participación (migración idempotente para tablas que ya la tenían).
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HR.tbl_ResearchProjectParticipations') AND name = 'StageTypeID')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ResearchPart_Stage')
        ALTER TABLE HR.tbl_ResearchProjectParticipations DROP CONSTRAINT FK_ResearchPart_Stage;
    ALTER TABLE HR.tbl_ResearchProjectParticipations DROP COLUMN StageTypeID;
END
GO

-- 3) Catálogos (Name = código estable; Description = texto para el usuario) ----
IF NOT EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = 'RESEARCH_PROJECT_ROLE')
    INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, CreatedAt, SortOrder) VALUES
        ('RESEARCH_PROJECT_ROLE', N'COORDINADOR_PRINCIPAL',  N'Coordinador Principal',  1, GETDATE(), 1),
        ('RESEARCH_PROJECT_ROLE', N'COORDINADOR_SUBROGANTE', N'Coordinador Subrogante', 1, GETDATE(), 2),
        ('RESEARCH_PROJECT_ROLE', N'INVESTIGADOR',           N'Investigador',           1, GETDATE(), 3);

-- La etapa de la participación (tiempo normal / prórroga) se eliminó: se deduce de las fechas. Sus valores
-- de catálogo quedan inactivos (no se borran por si algún día se retoma).
UPDATE HR.ref_Types SET IsActive = 0 WHERE Category = 'RESEARCH_PROJECT_STAGE' AND IsActive = 1;
GO

-- 3b) Directorio de archivos propio (RelativePath vacío: lo arma el backend; ver estándar de almacenamiento) -----
-- La carpeta física G:\ArchUta\HR\research_projects\ debe existir en el NAS con permisos para la cuenta de servicio.
IF NOT EXISTS (SELECT 1 FROM HR.tbl_DirectoryParameters WHERE Code = 'HR_RESEARCH_PROJECTS')
    INSERT INTO HR.tbl_DirectoryParameters (Code, PhysicalPath, RelativePath, Description, Extension, MaxSizeMB, Status, CreatedAt)
    VALUES ('HR_RESEARCH_PROJECTS', 'G:\ArchUta\HR\research_projects\', '', N'Resoluciones de proyectos de investigación (aprobación, prórroga y finalización)', '.pdf', 15, 1, GETDATE());
GO

-- 4) Rol y permisos ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM auth.tbl_Roles WHERE Name = 'R_PROYECTOS')
    INSERT INTO auth.tbl_Roles (Name, Description, IsActive, Priority, CreatedAt, IsDeleted)
    VALUES ('R_PROYECTOS', N'Gestión de proyectos de investigación y sus participantes', 1, 100, SYSDATETIME(), 0);

INSERT INTO auth.tbl_Permissions (Name, Module, Action, Description, Version, IsDeleted)
SELECT v.Name, 'RESEARCH_PROJECTS', v.Action, v.Description, 1, 0
FROM (VALUES
    ('RESEARCH_PROJECTS.READ',   'READ',   N'Consultar proyectos de investigación y sus participantes'),
    ('RESEARCH_PROJECTS.CREATE', 'CREATE', N'Registrar proyectos de investigación'),
    ('RESEARCH_PROJECTS.UPDATE', 'UPDATE', N'Editar proyectos de investigación y sus participantes'),
    ('RESEARCH_PROJECTS.DELETE', 'DELETE', N'Eliminar proyectos de investigación')
) AS v(Name, Action, Description)
WHERE NOT EXISTS (SELECT 1 FROM auth.tbl_Permissions p WHERE p.Name = v.Name AND p.IsDeleted = 0);

INSERT INTO auth.tbl_RolePermissions (RoleId, PermissionId, GrantedAt)
SELECT r.Id, p.Id, SYSDATETIME()
FROM auth.tbl_Roles r
JOIN auth.tbl_Permissions p ON p.Name LIKE 'RESEARCH_PROJECTS.%' AND p.IsDeleted = 0
WHERE r.Name IN ('R_PROYECTOS', 'Administrador') AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);
GO

-- 5) Menú: módulo raíz "Investigación" > "Proyectos de Investigación" ------------
-- Módulo propio (fuera de Talento Humano). Si la opción ya existía bajo Talento Humano, se mueve.
DECLARE @RootId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Name = N'Investigación' AND ParentId IS NULL AND IsDeleted = 0);

IF @RootId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    VALUES (N'Investigación', NULL, 'BookOpen', NULL, 4, 1, 'Investigacion', 0);
    SET @RootId = SCOPE_IDENTITY();
END

DECLARE @MenuId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Url = '/research-projects' AND IsDeleted = 0);

IF @MenuId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    VALUES (N'Proyectos de Investigación', '/research-projects', 'FileSearch', @RootId, 1, 1, 'Investigacion', 0);
    SET @MenuId = SCOPE_IDENTITY();
END
ELSE
    UPDATE auth.tbl_MenuItems SET ParentId = @RootId, [Order] = 1, ModuleName = 'Investigacion' WHERE Id = @MenuId;

INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
SELECT r.Id, m.Id, 1
FROM auth.tbl_Roles r
CROSS JOIN (SELECT @RootId AS Id UNION ALL SELECT @MenuId) m
WHERE r.Name IN ('Administrador', 'R_PROYECTOS')
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems rm WHERE rm.RoleId = r.Id AND rm.MenuItemId = m.Id);
GO
