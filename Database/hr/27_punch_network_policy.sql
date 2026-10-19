-- ============================================================================
-- 27_punch_network_policy.sql
-- Control de picadas por red: lista de IP/rangos del campus, permiso de picada externa
-- por empleado y registro de picadas fuera del campus.
--   HR.tbl_AllowedPunchNetworks  : IP o rangos CIDR del campus (administrados desde la pantalla).
--   HR.tbl_EmployeePunchPolicies : empleados autorizados a picar fuera del campus (motivo y vigencia).
--   HR.tbl_PunchAccessLog        : picadas externas permitidas / rechazadas / que se habrían rechazado.
-- Modo de operación en HR.tbl_Parameters (PUNCH_NETWORK_MODE): OFF | AUDIT | ENFORCE.
-- Se siembra en AUDIT: registra pero NO bloquea, para revisar a quién se bloquearía antes de activar.
-- Idempotente. Esquema auth compartido con RepositoryUta (permisos y menú).
-- ============================================================================

IF OBJECT_ID(N'HR.tbl_AllowedPunchNetworks', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_AllowedPunchNetworks (
        NetworkId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_AllowedPunchNetworks PRIMARY KEY,
        Name       NVARCHAR(120) NOT NULL,
        IpOrCidr   VARCHAR(64)   NOT NULL,
        Notes      NVARCHAR(300) NULL,
        IsActive   BIT           NOT NULL CONSTRAINT DF_tbl_AllowedPunchNetworks_IsActive DEFAULT (1),
        CreatedAt  DATETIME2     NOT NULL CONSTRAINT DF_tbl_AllowedPunchNetworks_CreatedAt DEFAULT (SYSDATETIME()),
        CreatedBy  INT           NULL,
        UpdatedAt  DATETIME2     NULL,
        UpdatedBy  INT           NULL
    );
    CREATE UNIQUE INDEX UX_tbl_AllowedPunchNetworks_IpOrCidr ON HR.tbl_AllowedPunchNetworks (IpOrCidr);
END
GO

IF OBJECT_ID(N'HR.tbl_EmployeePunchPolicies', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_EmployeePunchPolicies (
        PolicyId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmployeePunchPolicies PRIMARY KEY,
        EmployeeId         INT           NOT NULL,
        AllowExternalPunch BIT           NOT NULL CONSTRAINT DF_tbl_EmployeePunchPolicies_Allow DEFAULT (0),
        Reason             NVARCHAR(300) NULL,
        ValidFrom          DATE          NULL,
        ValidTo            DATE          NULL,
        AuthorizedBy       INT           NULL,
        CreatedAt          DATETIME2     NOT NULL CONSTRAINT DF_tbl_EmployeePunchPolicies_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt          DATETIME2     NULL,
        UpdatedBy          INT           NULL,
        CONSTRAINT FK_tbl_EmployeePunchPolicies_Employee FOREIGN KEY (EmployeeId) REFERENCES HR.tbl_Employees (EmployeeID),
        CONSTRAINT CK_tbl_EmployeePunchPolicies_Dates CHECK (ValidFrom IS NULL OR ValidTo IS NULL OR ValidTo >= ValidFrom)
    );
    CREATE UNIQUE INDEX UX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies (EmployeeId);
END
GO

IF OBJECT_ID(N'HR.tbl_PunchAccessLog', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_PunchAccessLog (
        LogId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_PunchAccessLog PRIMARY KEY,
        EmployeeId   INT          NOT NULL,
        AttemptedAt  DATETIME2    NOT NULL CONSTRAINT DF_tbl_PunchAccessLog_At DEFAULT (SYSDATETIME()),
        IpAddress    VARCHAR(64)  NULL,
        ForwardedFor VARCHAR(200) NULL,
        Result       VARCHAR(30)  NOT NULL,   -- EXTERNAL_ALLOWED | REJECTED | AUDIT_WOULD_REJECT
        Mode         VARCHAR(10)  NOT NULL,   -- AUDIT | ENFORCE
        Detail       NVARCHAR(200) NULL
    );
    CREATE INDEX IX_tbl_PunchAccessLog_At ON HR.tbl_PunchAccessLog (AttemptedAt DESC);
    CREATE INDEX IX_tbl_PunchAccessLog_Employee ON HR.tbl_PunchAccessLog (EmployeeId, AttemptedAt DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM HR.tbl_Parameters WHERE [name] = 'PUNCH_NETWORK_MODE')
    INSERT INTO HR.tbl_Parameters ([name], Pvalues, [Description], DataType, IsActive)
    VALUES ('PUNCH_NETWORK_MODE', 'AUDIT',
            N'Control de picadas por red: OFF (sin control), AUDIT (solo registra) o ENFORCE (bloquea fuera del campus)',
            'TEXTO', 1);
GO

-- Permisos de la pantalla: READ (consultar) y MANAGE (administrar redes y permisos de picada externa).
IF NOT EXISTS (SELECT 1 FROM auth.tbl_Permissions WHERE Name = 'PUNCH_POLICY.READ')
    INSERT INTO auth.tbl_Permissions (Name, Module, Action, Description, Version, IsDeleted)
    VALUES ('PUNCH_POLICY.READ', 'PUNCH_POLICY', 'READ', N'Consultar redes del campus, permisos de picada externa y su registro', 1, 0);
IF NOT EXISTS (SELECT 1 FROM auth.tbl_Permissions WHERE Name = 'PUNCH_POLICY.MANAGE')
    INSERT INTO auth.tbl_Permissions (Name, Module, Action, Description, Version, IsDeleted)
    VALUES ('PUNCH_POLICY.MANAGE', 'PUNCH_POLICY', 'MANAGE', N'Administrar redes del campus y autorizar picada externa', 1, 0);
GO

INSERT INTO auth.tbl_RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
SELECT r.Id, p.Id, SYSDATETIME(), 'script'
FROM auth.tbl_Roles r
JOIN auth.tbl_Permissions p ON p.Name IN ('PUNCH_POLICY.READ', 'PUNCH_POLICY.MANAGE')
WHERE r.IsDeleted = 0
  AND ((p.Name = 'PUNCH_POLICY.READ'   AND r.Name IN ('Administrador', 'R_RH', 'R_RH_ESPECIALISTA', 'R_RH_ANALISTA'))
    OR (p.Name = 'PUNCH_POLICY.MANAGE' AND r.Name IN ('Administrador', 'R_RH', 'R_RH_ESPECIALISTA')))
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RolePermissions x WHERE x.RoleId = r.Id AND x.PermissionId = p.Id);
GO

-- Opción de menú en Talento Humano.
DECLARE @ParentId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Name = N'Talento Humano' AND ParentId IS NULL AND IsDeleted = 0);
DECLARE @MenuId   INT;
IF @ParentId IS NULL
    THROW 50001, 'No se encontró el menú raíz Talento Humano.', 1;

SELECT @MenuId = Id FROM auth.tbl_MenuItems WHERE Url = '/attendance/punch-control' AND ParentId = @ParentId AND IsDeleted = 0;
IF @MenuId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    VALUES (N'Control de Picadas', '/attendance/punch-control', 'ShieldCheck', @ParentId,
            (SELECT ISNULL(MAX([Order]), 0) + 1 FROM auth.tbl_MenuItems WHERE ParentId = @ParentId AND IsDeleted = 0),
            1, 'TalentoHumano', 0);
    SET @MenuId = SCOPE_IDENTITY();
END

INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
SELECT r.Id, @MenuId, 1
FROM auth.tbl_Roles r
WHERE r.Name IN ('Administrador', 'R_RH', 'R_RH_ESPECIALISTA', 'R_RH_ANALISTA') AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems rm WHERE rm.RoleId = r.Id AND rm.MenuItemId = @MenuId);
GO

-- ============================================================================
-- Reparto de responsabilidades (2026-10-19)
--   PUNCH_NETWORK.MANAGE : lista de IP del campus y modo de control      -> DITIC y administradores técnicos
--   PUNCH_POLICY.MANAGE  : autorizar picada externa por empleado         -> administradores y Talento Humano
--   PUNCH_POLICY.READ    : consultar la pantalla y el registro
-- La misma pantalla (/attendance/punch-control) tiene dos entradas de menú: Administración (administradores y
-- DITIC) y Talento Humano (RRHH). Administrador solo la ve en Administración para no duplicarla.
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM auth.tbl_Permissions WHERE Name = 'PUNCH_NETWORK.MANAGE')
    INSERT INTO auth.tbl_Permissions (Name, Module, Action, Description, Version, IsDeleted)
    VALUES ('PUNCH_NETWORK.MANAGE', 'PUNCH_NETWORK', 'MANAGE', N'Administrar la lista de IP del campus y el modo de control de picadas', 1, 0);
GO

INSERT INTO auth.tbl_RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
SELECT r.Id, p.Id, SYSDATETIME(), 'script'
FROM auth.tbl_Roles r
JOIN auth.tbl_Permissions p ON p.Name IN ('PUNCH_POLICY.READ', 'PUNCH_POLICY.MANAGE', 'PUNCH_NETWORK.MANAGE')
WHERE r.IsDeleted = 0
  AND ((p.Name = 'PUNCH_POLICY.READ'    AND r.Name IN ('Administrador', 'Supervisor', 'Desarrollador', 'R_DITIC', 'R_RH', 'R_RH_ESPECIALISTA', 'R_RH_ANALISTA'))
    OR (p.Name = 'PUNCH_POLICY.MANAGE'  AND r.Name IN ('Administrador', 'Supervisor', 'Desarrollador', 'R_RH', 'R_RH_ESPECIALISTA'))
    OR (p.Name = 'PUNCH_NETWORK.MANAGE' AND r.Name IN ('Administrador', 'Desarrollador', 'R_DITIC')))
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RolePermissions x WHERE x.RoleId = r.Id AND x.PermissionId = p.Id);
GO

-- Entrada en Administración.
DECLARE @AdminId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Name = N'Administración' AND ParentId IS NULL AND IsDeleted = 0);
DECLARE @ThId    INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Name = N'Talento Humano'  AND ParentId IS NULL AND IsDeleted = 0);
DECLARE @AdminMenuId INT;
IF @AdminId IS NULL THROW 50002, 'No se encontró el menú raíz Administración.', 1;

SELECT @AdminMenuId = Id FROM auth.tbl_MenuItems WHERE Url = '/attendance/punch-control' AND ParentId = @AdminId AND IsDeleted = 0;
IF @AdminMenuId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    VALUES (N'Control de Picadas', '/attendance/punch-control', 'ShieldCheck', @AdminId,
            (SELECT ISNULL(MAX([Order]), 0) + 1 FROM auth.tbl_MenuItems WHERE ParentId = @AdminId AND IsDeleted = 0),
            1, 'Administracion', 0);
    SET @AdminMenuId = SCOPE_IDENTITY();
END

INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
SELECT r.Id, @AdminMenuId, 1
FROM auth.tbl_Roles r
WHERE r.Name IN ('Administrador', 'Supervisor', 'Desarrollador', 'R_DITIC') AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems rm WHERE rm.RoleId = r.Id AND rm.MenuItemId = @AdminMenuId);

-- R_DITIC tiene ítems de Administración asignados pero no la raíz: sin ella no vería el menú (hoy los 2 usuarios DITIC
-- también son Administrador/Desarrollador y por eso sí lo ven). Se le da visibilidad a la raíz.
INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
SELECT r.Id, @AdminId, 1
FROM auth.tbl_Roles r
WHERE r.Name = 'R_DITIC' AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems rm WHERE rm.RoleId = r.Id AND rm.MenuItemId = @AdminId);

-- Administrador la ve en Administración; se quita de la entrada de Talento Humano (que queda para RRHH).
DELETE rm
FROM auth.tbl_RoleMenuItems rm
JOIN auth.tbl_Roles r ON r.Id = rm.RoleId AND r.Name = 'Administrador'
JOIN auth.tbl_MenuItems m ON m.Id = rm.MenuItemId AND m.Url = '/attendance/punch-control' AND m.ParentId = @ThId;
GO

-- Intervalo mínimo entre dos picadas del mismo empleado (minutos; 0 = sin regla). Lo aplica el backend: el trigger
-- HR.trg_Punch_Validations que tenía esta regla está deshabilitado a propósito (bloquearía la importación de biométricos).
IF NOT EXISTS (SELECT 1 FROM HR.tbl_Parameters WHERE [name] = 'PUNCH_MIN_INTERVAL_MINUTES')
    INSERT INTO HR.tbl_Parameters ([name], Pvalues, [Description], DataType, IsActive)
    VALUES ('PUNCH_MIN_INTERVAL_MINUTES', '5', N'Minutos mínimos entre dos picadas del mismo empleado (0 = sin regla)', 'NUMERO', 1);
GO
