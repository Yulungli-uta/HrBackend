-- Mueve la opción de menú "Funcionario Régimen Laboral" fuera de /admin.
-- El frontend exige el rol Administrador a toda ruta bajo /admin (PermissionService.ROUTE_PERMISSIONS),
-- así que R_RH y R_RH_ANALISTA veían la opción pero recibían "Acceso denegado".
-- Idempotente. Después de ejecutarlo, quien tenga sesión abierta debe cerrar sesión y volver a entrar.
UPDATE auth.tbl_MenuItems
SET Url = N'/employee-labor-regimes'
WHERE Url = N'/admin/employee-labor-regimes';

SELECT Id, Name, Url FROM auth.tbl_MenuItems WHERE Url IN (N'/employee-labor-regimes', N'/admin/employee-labor-regimes');
