/* Capacitaciones: origen del registro y sincronización con DEaDV.
   Source:         'Manual' (digitado), 'DEaDV' (sincronizado) o 'Importado' (carga masiva 2026-09-23).
   ExternalId:     cert_code de DEaDV; clave para no duplicar al sincronizar.
   CertificateUrl: URL del certificado en DEaDV; nunca se expone al navegador (se sirve por proxy del backend).
   Idempotente. */
SET XACT_ABORT ON;
BEGIN TRAN;

IF COL_LENGTH('HR.tbl_Trainings', 'Source') IS NULL
    ALTER TABLE HR.tbl_Trainings ADD [Source] NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Trainings_Source DEFAULT ('Manual');

IF COL_LENGTH('HR.tbl_Trainings', 'ExternalId') IS NULL
    ALTER TABLE HR.tbl_Trainings ADD [ExternalId] NVARCHAR(50) NULL;

IF COL_LENGTH('HR.tbl_Trainings', 'CertificateUrl') IS NULL
    ALTER TABLE HR.tbl_Trainings ADD [CertificateUrl] NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Trainings_Source')
    EXEC('ALTER TABLE HR.tbl_Trainings ADD CONSTRAINT CK_Trainings_Source CHECK ([Source] IN (''Manual'', ''DEaDV'', ''Importado''))');

-- Carga masiva histórica: todas con 0 horas, creadas el 2026-09-23.
EXEC('UPDATE HR.tbl_Trainings SET [Source] = ''Importado''
      WHERE [Source] = ''Manual'' AND [Hours] = 0 AND CAST([CreatedAt] AS DATE) = ''2026-09-23''');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Trainings_Person_Source_ExternalId' AND object_id = OBJECT_ID('HR.tbl_Trainings'))
    EXEC('CREATE UNIQUE INDEX UX_Trainings_Person_Source_ExternalId
          ON HR.tbl_Trainings (PersonID, [Source], ExternalId) WHERE ExternalId IS NOT NULL');

COMMIT;
