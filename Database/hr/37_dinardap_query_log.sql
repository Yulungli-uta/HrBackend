-- ============================================================================
-- 37_dinardap_query_log.sql
-- Auditoría de consultas a DINARDAP (WsUtaDinardap.Api) hechas desde HrBackend: quién consultó (usuario conectado o
-- proceso del sistema), qué paquete (Registro Civil, Títulos, TCE), a qué identificación (ENMASCARADA: solo se conservan
-- los 3 primeros y los 2 últimos caracteres), cuándo y con qué resultado. No guarda datos devueltos por DINARDAP ni el token.
-- HrBackend sigue usando la cuenta de servicio para llamar a DINARDAP; este registro deja constancia de quién lo pidió.
-- Se escribe con Dapper desde HrBackend (mismo patrón que HR.tbl_JobExecutionLog) y se consulta con GET dinardap-query-log
-- (permiso AUDIT.READ). Idempotente. Esquema HR.
-- ============================================================================

IF OBJECT_ID('HR.tbl_DinardapQueryLog', 'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_DinardapQueryLog (
        LogID                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DinardapQueryLog PRIMARY KEY,
        QueriedAt            DATETIME2(3)  NOT NULL CONSTRAINT DF_DinardapQueryLog_QueriedAt DEFAULT (SYSDATETIME()),
        Origin               NVARCHAR(10)  NOT NULL,           -- USER (pantalla) | SYSTEM (trabajo o proceso sin usuario)
        UserId               UNIQUEIDENTIFIER NULL,            -- id del usuario en el sistema de autenticación
        UserEmail            NVARCHAR(256) NULL,
        EmployeeId           INT NULL,                         -- empleado del usuario conectado, si lo tiene
        Package              NVARCHAR(20)  NOT NULL,           -- REGISTRO_CIVIL | TITULOS | TCE
        IdentificationMasked NVARCHAR(20)  NOT NULL,           -- p. ej. 171*****39
        Outcome              NVARCHAR(15)  NOT NULL,           -- OK | UNAVAILABLE | FORBIDDEN (el usuario no tiene el permiso en DINARDAP)
        DurationMs           INT NULL,
        SourceIp             NVARCHAR(64)  NULL,
        Context              NVARCHAR(200) NULL,               -- plantilla de la ruta (sin la identificación) o nombre del proceso
        CONSTRAINT CK_DinardapQueryLog_Origin  CHECK (Origin  IN (N'USER', N'SYSTEM')),
        CONSTRAINT CK_DinardapQueryLog_Outcome CHECK (Outcome IN (N'OK', N'UNAVAILABLE', N'FORBIDDEN'))
    );
END
GO

-- 1b) El resultado FORBIDDEN (el usuario no tiene el rol de consulta en DINARDAP) se agregó después: migración para tablas ya creadas. Idempotente.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DinardapQueryLog_Outcome' AND definition LIKE '%FORBIDDEN%')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DinardapQueryLog_Outcome')
        ALTER TABLE HR.tbl_DinardapQueryLog DROP CONSTRAINT CK_DinardapQueryLog_Outcome;
    ALTER TABLE HR.tbl_DinardapQueryLog ADD CONSTRAINT CK_DinardapQueryLog_Outcome CHECK (Outcome IN (N'OK', N'UNAVAILABLE', N'FORBIDDEN'));
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DinardapQueryLog_QueriedAt' AND object_id = OBJECT_ID('HR.tbl_DinardapQueryLog'))
    CREATE INDEX IX_DinardapQueryLog_QueriedAt ON HR.tbl_DinardapQueryLog (QueriedAt DESC) INCLUDE (UserEmail, Package, Outcome);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DinardapQueryLog_User' AND object_id = OBJECT_ID('HR.tbl_DinardapQueryLog'))
    CREATE INDEX IX_DinardapQueryLog_User ON HR.tbl_DinardapQueryLog (UserId, QueriedAt DESC);
GO

-- 2) Menú: Reportes > "Consultas a DINARDAP" (/reports/dinardap-queries) ------------
-- Mismo criterio que "Auditoría de Reportes": visible para el rol Administrador (el endpoint exige AUDIT.READ).
-- Para otros roles, agregar la fila en auth.tbl_RoleMenuItems y asegurarse de que el rol tenga AUDIT.READ.
DECLARE @ReportsId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Url = N'/reports' AND ParentId IS NULL AND IsDeleted = 0);
DECLARE @MenuId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Url = N'/reports/dinardap-queries' AND IsDeleted = 0);

IF @ReportsId IS NOT NULL AND @MenuId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    VALUES (N'Consultas a DINARDAP', N'/reports/dinardap-queries', N'FileSpreadsheet', @ReportsId, 5, 1, N'Reports', 0);
    SET @MenuId = SCOPE_IDENTITY();
END

INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
SELECT r.Id, @MenuId, 1
FROM auth.tbl_Roles r
WHERE r.Name = N'Administrador' AND r.IsDeleted = 0 AND @MenuId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems rm WHERE rm.RoleId = r.Id AND rm.MenuItemId = @MenuId);
GO
