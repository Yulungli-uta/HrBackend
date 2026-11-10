-- ============================================================================
-- 38_dinardap_consulta_role.sql
-- Roles para que el TOKEN DEL USUARIO sea aceptado por WsUtaDinardap.Api (las pantallas de HrBackend reenvían el token de la persona
-- que consulta, ver Dinardap:ForwardUserToken). Un rol por paquete, para asignar según la necesidad:
--   R_DINARDAP_REGISTRO_CIVIL  -> DINARDAP.REGISTRO_CIVIL.READ  (alta de personas y cargas familiares; se asigna a RR.HH.)
--   R_DINARDAP_TITULOS         -> DINARDAP.TITULOS.READ          (formación académica / SENESCYT)
-- La sincronización SENESCYT de la hoja de vida NO usa el token del empleado: HrBackend comprueba que sea su propia persona
-- (EMPLOYEE_PROFILE.CREATE + dueño) y consulta con la cuenta de servicio. Por eso R_EMPLOYEE NO tiene DINARDAP.TITULOS.READ
-- (si lo tuviera, un empleado podría llamar a DINARDAP directo por la cédula de otra persona). TCE no se otorga a nadie.
-- No sustituye a R_DINARDAP_LEGACY (confianza por red de la DLL antigua), que no se toca. La cuenta de servicio de los procesos en
-- segundo plano (y la sincronización SENESCYT) debe tener R_DINARDAP_TITULOS o R_DINARDAP_LEGACY.
-- Reemplaza al rol R_DINARDAP_CONSULTA de una primera versión de este script (se retira si no tiene usuarios). Idempotente.
-- ============================================================================

SET NOCOUNT ON;
GO

-- 1) Roles --------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM auth.tbl_Roles WHERE Name = N'R_DINARDAP_REGISTRO_CIVIL')
    INSERT INTO auth.tbl_Roles (Name, Description, IsActive, Priority, CreatedAt, IsDeleted)
    VALUES (N'R_DINARDAP_REGISTRO_CIVIL', N'Consulta de Registro Civil (DINARDAP) con el token del propio usuario desde HrBackend', 1, 100, SYSDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM auth.tbl_Roles WHERE Name = N'R_DINARDAP_TITULOS')
    INSERT INTO auth.tbl_Roles (Name, Description, IsActive, Priority, CreatedAt, IsDeleted)
    VALUES (N'R_DINARDAP_TITULOS', N'Consulta de títulos / formación académica (DINARDAP-SENESCYT) con el token del propio usuario desde HrBackend', 1, 100, SYSDATETIME(), 0);
GO

-- Retira el rol de la primera versión si nadie lo usa (sus permisos quedan sin efecto al estar eliminado).
UPDATE auth.tbl_Roles SET IsActive = 0, IsDeleted = 1
WHERE Name = N'R_DINARDAP_CONSULTA' AND IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_UserRoles ur WHERE ur.RoleId = auth.tbl_Roles.Id AND ur.IsDeleted = 0);
GO

-- 2) Permisos -----------------------------------------------------------------
INSERT INTO auth.tbl_RolePermissions (RoleId, PermissionId, GrantedAt)
SELECT r.Id, p.Id, SYSDATETIME()
FROM (VALUES
    (N'R_DINARDAP_REGISTRO_CIVIL', N'DINARDAP.REGISTRO_CIVIL.READ'),
    (N'R_DINARDAP_TITULOS',        N'DINARDAP.TITULOS.READ')
) AS g (RoleName, PermissionName)
JOIN auth.tbl_Roles r ON r.Name = g.RoleName AND r.IsDeleted = 0
JOIN auth.tbl_Permissions p ON p.Name = g.PermissionName AND p.IsDeleted = 0
WHERE NOT EXISTS (SELECT 1 FROM auth.tbl_RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

-- Retira el permiso que una versión anterior de este script dio a R_EMPLOYEE.
DELETE rp
FROM auth.tbl_RolePermissions rp
JOIN auth.tbl_Roles r ON r.Id = rp.RoleId AND r.Name = N'R_EMPLOYEE'
JOIN auth.tbl_Permissions p ON p.Id = rp.PermissionId AND p.Name = N'DINARDAP.TITULOS.READ';
GO

-- 3) Asignación a RR.HH. (quienes hoy tienen DINARDAP_HR.READ; se excluyen cuentas de prueba @test.local) ----------
DECLARE @AsignarARrhh BIT = 1;   -- 0 = no asigna a nadie

IF @AsignarARrhh = 1
BEGIN
    DECLARE @RcId INT = (SELECT Id FROM auth.tbl_Roles WHERE Name = N'R_DINARDAP_REGISTRO_CIVIL' AND IsDeleted = 0);
    DECLARE @TitId INT = (SELECT Id FROM auth.tbl_Roles WHERE Name = N'R_DINARDAP_TITULOS' AND IsDeleted = 0);

    DECLARE @Holders TABLE (UserId UNIQUEIDENTIFIER PRIMARY KEY);
    INSERT INTO @Holders (UserId)
    SELECT DISTINCT u.Id
    FROM auth.tbl_Users u
    JOIN auth.tbl_UserRoles ur ON ur.UserId = u.Id AND ur.IsDeleted = 0 AND (ur.ExpiresAt IS NULL OR ur.ExpiresAt > SYSDATETIME())
    JOIN auth.tbl_RolePermissions rp ON rp.RoleId = ur.RoleId
    JOIN auth.tbl_Permissions p ON p.Id = rp.PermissionId AND p.Name = N'DINARDAP_HR.READ' AND p.IsDeleted = 0
    WHERE u.IsActive = 1 AND u.Email NOT LIKE N'%@test.local';

    -- Registro Civil: todo RR.HH.
    INSERT INTO auth.tbl_UserRoles (UserId, RoleId, AssignedAt, Reason, IsDeleted)
    SELECT h.UserId, @RcId, SYSDATETIME(), N'Consulta DINARDAP con token del usuario (script 38)', 0
    FROM @Holders h
    WHERE NOT EXISTS (SELECT 1 FROM auth.tbl_UserRoles x WHERE x.UserId = h.UserId AND x.RoleId = @RcId AND x.IsDeleted = 0);

    -- Retira el rol de títulos que una versión anterior asignó a RR.HH. (ya no se usa con token de usuario).
    UPDATE x SET IsDeleted = 1
    FROM auth.tbl_UserRoles x
    WHERE x.RoleId = @TitId AND x.IsDeleted = 0 AND x.Reason = N'Consulta DINARDAP con token del usuario (script 38)';
END
GO
