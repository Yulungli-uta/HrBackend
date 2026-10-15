-- ============================================================================
-- 25_time_planning_overview_menu.sql
-- Pantalla "Planificación Horas Extra" (consulta de planes de horas extra con sus
-- empleados y estados) en el módulo Talento Humano.
-- Reutiliza el permiso existente TIME_PLANNING.READ (sin permiso nuevo): solo se
-- asigna a los roles de Recursos Humanos que aún no lo tienen. Idempotente.
-- Esquema auth (compartido con RepositoryUta).
-- ============================================================================

DECLARE @ParentId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Name = N'Talento Humano' AND ParentId IS NULL AND IsDeleted = 0);
DECLARE @MenuId   INT;

IF @ParentId IS NULL
    THROW 50001, 'No se encontró el menú raíz Talento Humano.', 1;

SELECT @MenuId = Id FROM auth.tbl_MenuItems WHERE Url = '/overtime-planning' AND IsDeleted = 0;

IF @MenuId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    VALUES (N'Planificación Horas Extra', '/overtime-planning', 'Timer', @ParentId,
            (SELECT ISNULL(MAX([Order]), 0) + 1 FROM auth.tbl_MenuItems WHERE ParentId = @ParentId AND IsDeleted = 0),
            1, 'TalentoHumano', 0);
    SET @MenuId = SCOPE_IDENTITY();
END

-- Roles de Recursos Humanos que ven la opción: Administrador, R_RH, R_RH_ANALISTA, R_RH_ESPECIALISTA.
INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
SELECT r.Id, @MenuId, 1
FROM auth.tbl_Roles r
WHERE r.Name IN ('Administrador', 'R_RH', 'R_RH_ANALISTA', 'R_RH_ESPECIALISTA')
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems rm WHERE rm.RoleId = r.Id AND rm.MenuItemId = @MenuId);

-- Permiso TIME_PLANNING.READ para esos mismos roles (hoy solo lo tiene R_JEFE_INMEDIATO).
DECLARE @ReadPermissionId INT = (SELECT Id FROM auth.tbl_Permissions WHERE Name = 'TIME_PLANNING.READ' AND IsDeleted = 0);

IF @ReadPermissionId IS NOT NULL
    INSERT INTO auth.tbl_RolePermissions (RoleId, PermissionId, GrantedAt)
    SELECT r.Id, @ReadPermissionId, SYSDATETIME()
    FROM auth.tbl_Roles r
    WHERE r.Name IN ('Administrador', 'R_RH', 'R_RH_ANALISTA', 'R_RH_ESPECIALISTA')
      AND NOT EXISTS (SELECT 1 FROM auth.tbl_RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = @ReadPermissionId);
GO
