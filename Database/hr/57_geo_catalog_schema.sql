-- Catálogo geográfico (países, provincias, cantones): columnas para cruzar con fuentes externas (ISO 3166 / GeoNames),
-- marca de activo y una tabla de registro de los cambios de limpieza. No cambia columnas existentes ni la PK/FK.
SET XACT_ABORT ON;
BEGIN TRAN;

IF COL_LENGTH('HR.tbl_Countries','IsoCode') IS NULL ALTER TABLE HR.tbl_Countries ADD IsoCode nvarchar(10) NULL;
IF COL_LENGTH('HR.tbl_Countries','Source') IS NULL ALTER TABLE HR.tbl_Countries ADD Source nvarchar(20) NULL;
IF COL_LENGTH('HR.tbl_Countries','IsActive') IS NULL ALTER TABLE HR.tbl_Countries ADD IsActive bit NOT NULL CONSTRAINT DF_Countries_IsActive DEFAULT 1;
IF COL_LENGTH('HR.tbl_Provinces','IsoCode') IS NULL ALTER TABLE HR.tbl_Provinces ADD IsoCode nvarchar(20) NULL;
IF COL_LENGTH('HR.tbl_Provinces','Source') IS NULL ALTER TABLE HR.tbl_Provinces ADD Source nvarchar(20) NULL;
IF COL_LENGTH('HR.tbl_Provinces','IsActive') IS NULL ALTER TABLE HR.tbl_Provinces ADD IsActive bit NOT NULL CONSTRAINT DF_Provinces_IsActive DEFAULT 1;
IF COL_LENGTH('HR.tbl_Cantons','IsoCode') IS NULL ALTER TABLE HR.tbl_Cantons ADD IsoCode nvarchar(30) NULL;
IF COL_LENGTH('HR.tbl_Cantons','Source') IS NULL ALTER TABLE HR.tbl_Cantons ADD Source nvarchar(20) NULL;
IF COL_LENGTH('HR.tbl_Cantons','IsActive') IS NULL ALTER TABLE HR.tbl_Cantons ADD IsActive bit NOT NULL CONSTRAINT DF_Cantons_IsActive DEFAULT 1;

IF OBJECT_ID('HR.tbl_GeoCleanupLog') IS NULL
CREATE TABLE HR.tbl_GeoCleanupLog (
    LogId       int IDENTITY(1,1) CONSTRAINT PK_GeoCleanupLog PRIMARY KEY,
    TableName   nvarchar(60)  NOT NULL,
    PkValue     nvarchar(50)  NOT NULL,
    ColumnName  nvarchar(60)  NOT NULL,
    OldValue    nvarchar(50)  NULL,
    NewValue    nvarchar(50)  NULL,
    Reason      nvarchar(100) NOT NULL,
    LoggedAt    datetime2     NOT NULL CONSTRAINT DF_GeoCleanupLog_At DEFAULT SYSDATETIME()
);
COMMIT;

-- Índices únicos filtrados (se crean fuera de la transacción de columnas: la columna debe existir al compilar el lote)
EXEC(N'IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ''UX_Countries_IsoCode'') CREATE UNIQUE INDEX UX_Countries_IsoCode ON HR.tbl_Countries(IsoCode) WHERE IsoCode IS NOT NULL');
EXEC(N'IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ''UX_Provinces_IsoCode'') CREATE UNIQUE INDEX UX_Provinces_IsoCode ON HR.tbl_Provinces(IsoCode) WHERE IsoCode IS NOT NULL');
EXEC(N'IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ''UX_Cantons_IsoCode'') CREATE UNIQUE INDEX UX_Cantons_IsoCode ON HR.tbl_Cantons(IsoCode) WHERE IsoCode IS NOT NULL');
-- Los registros que ya existían quedan marcados como heredados
EXEC(N'UPDATE HR.tbl_Countries SET Source = N''LEGACY'' WHERE Source IS NULL; UPDATE HR.tbl_Provinces SET Source = N''LEGACY'' WHERE Source IS NULL; UPDATE HR.tbl_Cantons SET Source = N''LEGACY'' WHERE Source IS NULL;');
EXEC(N'SELECT (SELECT COUNT(*) FROM HR.tbl_Countries WHERE Source = N''LEGACY'') countries, (SELECT COUNT(*) FROM HR.tbl_Provinces WHERE Source = N''LEGACY'') provinces, (SELECT COUNT(*) FROM HR.tbl_Cantons WHERE Source = N''LEGACY'') cantons');
