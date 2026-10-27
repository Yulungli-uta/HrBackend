-- Preferencias de interfaz por usuario (página de inicio, tamaño de letra, tema, menú).
-- UserId = auth.tbl_Users.Id (claim "sub"); sin FK a propósito: auth lo administra RepositoryUta.
-- Idempotente.
IF OBJECT_ID(N'HR.tbl_UserPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_UserPreferences (
        UserPreferenceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_UserPreferences PRIMARY KEY,
        UserId           UNIQUEIDENTIFIER  NOT NULL,
        SchemaVersion    SMALLINT          NOT NULL CONSTRAINT DF_tbl_UserPreferences_SchemaVersion DEFAULT (1),
        PreferencesJson  NVARCHAR(4000)    NOT NULL,
        CreatedAt        DATETIME2         NOT NULL CONSTRAINT DF_tbl_UserPreferences_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt        DATETIME2         NOT NULL CONSTRAINT DF_tbl_UserPreferences_UpdatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT CK_tbl_UserPreferences_Json CHECK (ISJSON(PreferencesJson) = 1)
    );
    CREATE UNIQUE INDEX UX_tbl_UserPreferences_UserId ON HR.tbl_UserPreferences (UserId);
END
GO
