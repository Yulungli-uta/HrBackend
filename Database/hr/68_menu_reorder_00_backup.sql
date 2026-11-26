/* =====================================================================
   68_menu_reorder_00_backup.sql
   Respaldo previo a 68_menu_reorder_01_apply.sql: copia exacta de auth.tbl_MenuItems y auth.tbl_RoleMenuItems.
   No sobrescribe si ya existen. La BD es compartida con producción.
   ===================================================================== */
SET NOCOUNT ON;
IF OBJECT_ID(N'auth.bak_MenuItems_20261126', N'U') IS NULL
    SELECT * INTO auth.bak_MenuItems_20261126 FROM auth.tbl_MenuItems;
IF OBJECT_ID(N'auth.bak_RoleMenuItems_20261126', N'U') IS NULL
    SELECT * INTO auth.bak_RoleMenuItems_20261126 FROM auth.tbl_RoleMenuItems;
SELECT 'tbl_MenuItems' AS tabla, (SELECT COUNT(*) FROM auth.tbl_MenuItems) AS origen, (SELECT COUNT(*) FROM auth.bak_MenuItems_20261126) AS respaldo
UNION ALL SELECT 'tbl_RoleMenuItems', (SELECT COUNT(*) FROM auth.tbl_RoleMenuItems), (SELECT COUNT(*) FROM auth.bak_RoleMenuItems_20261126);
