-- ============================================================================
-- 53_stored_file_versions.sql
-- Versionado genérico de archivos (HR.TBL_StoredFile) por "ranura" de un registro.
-- Reemplazar un archivo no lo sobrescribe: crea una versión nueva, deja la anterior inactiva
-- (quién, cuándo y motivo) y archiva su StoredFile (Status = 3). El archivo físico se conserva.
--   HR.tbl_StoredFileVersions: una fila por versión.
--     EntityType/EntityId: el registro dueño, con los mismos valores que HR.TBL_StoredFile (ej. 'PersonnelAction' / '123').
--     SlotCode: la ranura lógica dentro del registro (ej. 'SIGNED' = documento firmado). Un registro puede tener varias ranuras.
--   La versión vigente (IsCurrent = 1) es la misma a la que apunta el puntero propio de cada módulo
--   (ej. SignedDocumentStoredFileId), por lo que vistas y pantallas existentes no cambian.
-- Carga inicial: cada firmado ya vinculado y activo de acciones y contratos queda como versión 1 de la ranura 'SIGNED'.
-- Motivo obligatorio desde la versión 2. Reemplaza la tabla HR.tbl_SignedDocumentVersions de la primera versión de este script
-- (nunca fue usada por código). Idempotente.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('HR.tbl_SignedDocumentVersions') IS NOT NULL
    DROP TABLE HR.tbl_SignedDocumentVersions;
GO

IF OBJECT_ID('HR.tbl_StoredFileVersions') IS NULL
BEGIN
    CREATE TABLE HR.tbl_StoredFileVersions (
        VersionId     INT IDENTITY(1,1) NOT NULL,
        EntityType    NVARCHAR(50)  NOT NULL,
        EntityId      NVARCHAR(50)  NOT NULL,
        SlotCode      VARCHAR(30)   NOT NULL,
        VersionNo     INT           NOT NULL,
        StoredFileId  INT           NOT NULL,
        IsCurrent     BIT           NOT NULL CONSTRAINT DF_StoredFileVersions_IsCurrent DEFAULT (1),
        UploadedBy    INT           NULL,
        UploadedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_StoredFileVersions_UploadedAt DEFAULT (SYSDATETIME()),
        ReplacedBy    INT           NULL,
        ReplacedAt    DATETIME2(0)  NULL,
        Reason        NVARCHAR(500) NULL,
        CONSTRAINT PK_StoredFileVersions PRIMARY KEY CLUSTERED (VersionId),
        CONSTRAINT FK_StoredFileVersions_StoredFile FOREIGN KEY (StoredFileId) REFERENCES HR.TBL_StoredFile (FileId),
        CONSTRAINT CK_StoredFileVersions_VersionNo CHECK (VersionNo >= 1),
        -- Desde la versión 2 el motivo es obligatorio (la 1 es la carga inicial).
        CONSTRAINT CK_StoredFileVersions_Reason CHECK (VersionNo = 1 OR LEN(LTRIM(RTRIM(ISNULL(Reason, N'')))) > 0),
        -- Una versión inactiva debe saber cuándo fue reemplazada; una vigente no.
        CONSTRAINT CK_StoredFileVersions_Replaced CHECK ((IsCurrent = 1 AND ReplacedAt IS NULL) OR (IsCurrent = 0 AND ReplacedAt IS NOT NULL))
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StoredFileVersions_Slot_VersionNo' AND object_id = OBJECT_ID('HR.tbl_StoredFileVersions'))
    CREATE UNIQUE INDEX UX_StoredFileVersions_Slot_VersionNo
        ON HR.tbl_StoredFileVersions (EntityType, EntityId, SlotCode, VersionNo);
GO

-- Una sola versión vigente por ranura.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StoredFileVersions_Slot_Current' AND object_id = OBJECT_ID('HR.tbl_StoredFileVersions'))
    CREATE UNIQUE INDEX UX_StoredFileVersions_Slot_Current
        ON HR.tbl_StoredFileVersions (EntityType, EntityId, SlotCode)
        WHERE IsCurrent = 1;
GO

-- Un mismo archivo no puede ser versión de dos ranuras ni dos veces de la misma.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StoredFileVersions_StoredFile' AND object_id = OBJECT_ID('HR.tbl_StoredFileVersions'))
    CREATE UNIQUE INDEX UX_StoredFileVersions_StoredFile
        ON HR.tbl_StoredFileVersions (StoredFileId);
GO

-- Carga inicial: firmados ya vinculados (archivo activo) como versión 1 vigente de la ranura 'SIGNED'.
INSERT INTO HR.tbl_StoredFileVersions (EntityType, EntityId, SlotCode, VersionNo, StoredFileId, IsCurrent, UploadedBy, UploadedAt)
SELECT N'PersonnelAction', CAST(a.ActionId AS NVARCHAR(50)), 'SIGNED', 1, sf.FileId, 1, sf.CreatedBy, COALESCE(sf.CreatedAt, SYSDATETIME())
FROM HR.tbl_PersonnelActions a
JOIN HR.TBL_StoredFile sf
  ON sf.FileId = a.SignedDocumentStoredFileId
 AND sf.Status = 1
 AND (sf.DeletedAt IS NULL OR sf.DeletedAt < '1900-01-01')
WHERE a.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_StoredFileVersions v WHERE v.EntityType = N'PersonnelAction' AND v.EntityId = CAST(a.ActionId AS NVARCHAR(50)) AND v.SlotCode = 'SIGNED')
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_StoredFileVersions v WHERE v.StoredFileId = sf.FileId);
GO

INSERT INTO HR.tbl_StoredFileVersions (EntityType, EntityId, SlotCode, VersionNo, StoredFileId, IsCurrent, UploadedBy, UploadedAt)
SELECT N'HRCONTRACT', CAST(c.ContractID AS NVARCHAR(50)), 'SIGNED', 1, sf.FileId, 1, sf.CreatedBy, COALESCE(sf.CreatedAt, SYSDATETIME())
FROM HR.tbl_Contracts c
JOIN HR.TBL_StoredFile sf
  ON sf.FileId = c.SignedDocumentStoredFileID
 AND sf.Status = 1
 AND (sf.DeletedAt IS NULL OR sf.DeletedAt < '1900-01-01')
WHERE NOT EXISTS (SELECT 1 FROM HR.tbl_StoredFileVersions v WHERE v.EntityType = N'HRCONTRACT' AND v.EntityId = CAST(c.ContractID AS NVARCHAR(50)) AND v.SlotCode = 'SIGNED')
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_StoredFileVersions v WHERE v.StoredFileId = sf.FileId);
GO

-- Resumen de la carga inicial.
SELECT EntityType, SlotCode, COUNT(*) AS Versions, SUM(CAST(IsCurrent AS INT)) AS CurrentVersions
FROM HR.tbl_StoredFileVersions
GROUP BY EntityType, SlotCode;
GO
