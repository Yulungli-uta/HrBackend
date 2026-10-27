-- ============================================================================
-- 29_punch_detail_report_menu.sql
-- Opción de menú del "Reporte detallado de picadas" (/reports/punch-detail) bajo
-- Reportes > Asistencia. Copia los roles del reporte de Novedades de Asistencia.
-- El endpoint usa el permiso existente REPORTS.READ (sin permiso nuevo). Idempotente.
-- Esquema auth (compartido con RepositoryUta).
-- ============================================================================

DECLARE @SourceId INT = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Url = '/reports/attendance-novelties' AND IsDeleted = 0);
DECLARE @MenuId   INT;

IF @SourceId IS NULL
    THROW 50001, 'No se encontró el menú de referencia /reports/attendance-novelties.', 1;

SELECT @MenuId = Id FROM auth.tbl_MenuItems WHERE Url = '/reports/punch-detail' AND IsDeleted = 0;

IF @MenuId IS NULL
BEGIN
    INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
    SELECT N'RptDetallePicadas', '/reports/punch-detail', 'Fingerprint', ParentId,
           (SELECT ISNULL(MAX([Order]), 0) + 1 FROM auth.tbl_MenuItems WHERE ParentId = s.ParentId AND IsDeleted = 0),
           1, ModuleName, 0
    FROM auth.tbl_MenuItems s WHERE s.Id = @SourceId;
    SET @MenuId = SCOPE_IDENTITY();
END

INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
SELECT rm.RoleId, @MenuId, rm.IsVisible
FROM auth.tbl_RoleMenuItems rm
WHERE rm.MenuItemId = @SourceId
  AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems x WHERE x.RoleId = rm.RoleId AND x.MenuItemId = @MenuId);
GO
