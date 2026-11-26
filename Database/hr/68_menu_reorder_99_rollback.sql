/* =====================================================================
   68_menu_reorder_99_rollback.sql
   Reversión de 68_menu_reorder_01_apply.sql: devuelve padre y orden de cada ítem desde el respaldo y elimina los grupos creados.
   Una sola transacción; aborta si el resultado no es idéntico al respaldo.
   ===================================================================== */
SET NOCOUNT ON; SET XACT_ABORT ON;
IF OBJECT_ID(N'auth.bak_MenuItems_20261126', N'U') IS NULL THROW 50300, 'No existe el respaldo de tbl_MenuItems.', 1;
BEGIN TRAN;
UPDATE m SET m.ParentId = b.ParentId, m.[Order] = b.[Order]
FROM auth.tbl_MenuItems m JOIN auth.bak_MenuItems_20261126 b ON b.Id = m.Id
WHERE ISNULL(m.ParentId,-1) <> ISNULL(b.ParentId,-1) OR m.[Order] <> b.[Order];
IF EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems r WHERE r.MenuItemId NOT IN (SELECT Id FROM auth.bak_MenuItems_20261126))
    THROW 50301, 'Hay roles asignados a grupos nuevos: revisar antes de eliminarlos.', 1;
DELETE FROM auth.tbl_MenuItems WHERE Id NOT IN (SELECT Id FROM auth.bak_MenuItems_20261126);
IF EXISTS (SELECT Id, Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted FROM auth.tbl_MenuItems
           EXCEPT SELECT Id, Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted FROM auth.bak_MenuItems_20261126)
   OR EXISTS (SELECT Id, Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted FROM auth.bak_MenuItems_20261126
           EXCEPT SELECT Id, Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted FROM auth.tbl_MenuItems)
    THROW 50302, 'La reversión no dejó tbl_MenuItems idéntica al respaldo.', 1;
COMMIT TRAN;
PRINT 'REVERSIÓN APLICADA.';
