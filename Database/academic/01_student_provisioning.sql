-- ============================================================
-- Módulo Académico: Seguimiento de aprovisionamiento AD
-- Schema: HR (mismo DB que tbl_Students y tbl_StudentEnrollments)
-- El estado vive en HrBackend; RepositoryUta solo opera en AD.
-- ============================================================

-- Prerequisito: tbl_Students debe existir.

IF OBJECT_ID('[HR].[tbl_StudentProvisioning]') IS NULL
CREATE TABLE [HR].[tbl_StudentProvisioning] (
    [Id]                     UNIQUEIDENTIFIER DEFAULT (NEWID()) NOT NULL,
    [StudentId]              INT              NOT NULL,
    [Email]                  NVARCHAR(320)    NOT NULL,
    [DisplayName]            NVARCHAR(256)    NOT NULL,
    [GivenName]              NVARCHAR(128)    NULL,
    [Surname]                NVARCHAR(128)    NULL,
    [ProvisioningStatusId]   INT              NOT NULL DEFAULT (3001),
    [ProvisioningStatusName] NVARCHAR(64)     NULL,
    [AdObjectId]             NVARCHAR(256)    NULL,
    [SourceReference]        NVARCHAR(256)    NULL,
    [ErrorMessage]           NVARCHAR(MAX)    NULL,
    [RequestedBy]            NVARCHAR(320)    NULL,
    [ProvisionedAt]          DATETIME2        NULL,
    [DisabledAt]             DATETIME2        NULL,
    [CreatedAt]              DATETIME2        DEFAULT (SYSUTCDATETIME()) NOT NULL,
    [UpdatedAt]              DATETIME2        NULL,
    CONSTRAINT [PK_StudentProvisioning]  PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentProv_Students] FOREIGN KEY ([StudentId])
        REFERENCES [HR].[tbl_Students]([StudentId])
        ON DELETE NO ACTION
);
GO

-- Índices de consulta frecuente
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProvisioning_StudentId' AND object_id = OBJECT_ID('[HR].[tbl_StudentProvisioning]'))
    CREATE INDEX [IX_StudentProvisioning_StudentId]
        ON [HR].[tbl_StudentProvisioning] ([StudentId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProvisioning_StatusId' AND object_id = OBJECT_ID('[HR].[tbl_StudentProvisioning]'))
    CREATE INDEX [IX_StudentProvisioning_StatusId]
        ON [HR].[tbl_StudentProvisioning] ([ProvisioningStatusId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProvisioning_AdObjectId' AND object_id = OBJECT_ID('[HR].[tbl_StudentProvisioning]'))
    CREATE INDEX [IX_StudentProvisioning_AdObjectId]
        ON [HR].[tbl_StudentProvisioning] ([AdObjectId])
        WHERE [AdObjectId] IS NOT NULL;
GO
