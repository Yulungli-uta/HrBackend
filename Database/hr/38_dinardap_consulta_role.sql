-- ============================================================================
-- 38_dinardap_consulta_role.sql
-- Roles para que el TOKEN DEL USUARIO sea aceptado por WsUtaDinardap.Api (las pantallas de HrBackend reenvían el token de la persona
-- que consulta, ver Dinardap:ForwardUserToken). Un rol por paquete, para asignar según la necesidad:
--   R_DINARDAP_REGISTRO_CIVIL  -> DINARDAP.REGISTRO_CIVIL.READ  (alta de personas y cargas familiares; se asigna a RR.HH.)
--   R_DINARDAP_TITULOS         -> DINARDAP.TITULOS.READ          (formación académica / SENESCYT)
-- Además DINARDAP.TITULOS.READ se otorga al rol R_EMPLOYEE (decisión del usuario 2026-11-10: la sincronización SENESCYT desde pantalla
-- usa el token del propio empleado; HrBackend limita a que solo consulte su propia persona). TCE (sufragio) no se otorga a nadie.
-- No sustituye a R_DINARDAP_LEGACY (confianza por red de la DLL antigua), que no se toca. La cuenta de servicio de los procesos en
-- segundo plano debe tener R_DINARDAP_TITULOS (o R_DINARDAP_LEGACY).
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
    (N'R_DINARDAP_TITULOS',        N'DINARDAP.TITULOS.READ'),
    (N'R_EMPLOYEE',                N'DINARDAP.TITULOS.READ')
) AS g (RoleName, PermissionName)
JOIN auth.tbl_Roles r ON r.Name = g.RoleName AND r.IsDeleted = 0
JOIN auth.tbl_Permissions p ON p.Name = g.PermissionName AND p.IsDeleted = 0
WHERE NOT EXISTS (SELECT 1 FROM auth.tbl_RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

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

    -- Reactiva asignaciones que una versión anterior de este script dio de baja (la clave es UserId+RoleId).
    UPDATE x SET IsDeleted = 0
    FROM auth.tbl_UserRoles x
    JOIN @Holders h ON h.UserId = x.UserId
    WHERE x.RoleId IN (@RcId, @TitId) AND x.IsDeleted = 1 AND x.Reason = N'Consulta DINARDAP con token del usuario (script 38)'
      AND NOT (x.RoleId = @TitId AND EXISTS (SELECT 1 FROM auth.tbl_UserRoles y JOIN auth.tbl_Roles rr ON rr.Id = y.RoleId
                                             WHERE y.UserId = x.UserId AND y.IsDeleted = 0 AND rr.Name = N'R_EMPLOYEE'));

    -- Registro Civil: todo RR.HH.
    INSERT INTO auth.tbl_UserRoles (UserId, RoleId, AssignedAt, Reason, IsDeleted)
    SELECT h.UserId, @RcId, SYSDATETIME(), N'Consulta DINARDAP con token del usuario (script 38)', 0
    FROM @Holders h
    WHERE NOT EXISTS (SELECT 1 FROM auth.tbl_UserRoles x WHERE x.UserId = h.UserId AND x.RoleId = @RcId);

    -- Títulos: solo a quien de RR.HH. no tenga ya el permiso por R_EMPLOYEE.
    INSERT INTO auth.tbl_UserRoles (UserId, RoleId, AssignedAt, Reason, IsDeleted)
    SELECT h.UserId, @TitId, SYSDATETIME(), N'Consulta DINARDAP con token del usuario (script 38)', 0
    FROM @Holders h
    WHERE NOT EXISTS (SELECT 1 FROM auth.tbl_UserRoles x JOIN auth.tbl_Roles rr ON rr.Id = x.RoleId
                      WHERE x.UserId = h.UserId AND x.IsDeleted = 0 AND rr.Name IN (N'R_EMPLOYEE', N'R_DINARDAP_TITULOS'));
END
GO
