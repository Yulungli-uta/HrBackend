/* =====================================================================
   68_menu_reorder_01_apply.sql
   Reordena el menú de navegación (auth.tbl_MenuItems) y lo agrupa en 19 grupos nuevos. NO borra ni oculta ninguna fila,
   no cambia URLs, nombres ni visibilidad: solo ParentId y Order de los ítems existentes (renumerados de 10 en 10) y los grupos nuevos.
   Los roles no se tocan: el menú de cada usuario incluye la cadena de padres de sus ítems (auth.fn_MenuByUser).

   Uso (sqlcmd):  -v Commit=0  simula y revierte;  -v Commit=1  aplica (exige el respaldo de 68_menu_reorder_00_backup.sql).
   Verificaciones (aborta con THROW si alguna falla): ninguna fila borrada ni modificada salvo ParentId/Order, las páginas visibles de CADA rol
   son exactamente las mismas antes y después, ningún ítem visible queda sin su padre, órdenes únicos entre hermanos, todos los ítems
   llegan a una raíz y ningún grupo nuevo queda vacío.
   ===================================================================== */
SET NOCOUNT ON; SET XACT_ABORT ON;
IF '$(Commit)' NOT IN ('0','1') THROW 50310, 'Commit debe ser 0 o 1.', 1;
IF '$(Commit)' = '1' AND (OBJECT_ID(N'auth.bak_MenuItems_20261126', N'U') IS NULL OR OBJECT_ID(N'auth.bak_RoleMenuItems_20261126', N'U') IS NULL)
    THROW 50311, 'Falta el respaldo: ejecutar 68_menu_reorder_00_backup.sql antes.', 1;
IF EXISTS (SELECT 1 FROM auth.tbl_MenuItems WHERE Name = N'Seguridad y accesos' AND IsDeleted = 0)
    THROW 50312, 'El reordenamiento ya fue aplicado.', 1;

-- Estado previo (se crea antes de la transacción: sobrevive al ROLLBACK)
SELECT Id, Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted INTO #snap FROM auth.tbl_MenuItems;
DECLARE @rolesRows INT = (SELECT COUNT(*) FROM auth.tbl_RoleMenuItems);
CREATE TABLE #before (RoleId INT, ItemId INT);
CREATE TABLE #after  (RoleId INT, ItemId INT);

-- Menú resultante por rol: ítems asignados (visibles) + cadena de padres visibles (misma lógica que auth.fn_MenuByUser)
WITH base AS (
    SELECT r.RoleId, m.Id, m.ParentId FROM auth.tbl_RoleMenuItems r JOIN auth.tbl_MenuItems m ON m.Id = r.MenuItemId
    WHERE r.IsVisible = 1 AND m.IsVisible = 1 AND m.IsDeleted = 0),
chain AS (
    SELECT RoleId, Id, ParentId FROM base
    UNION ALL
    SELECT c.RoleId, p.Id, p.ParentId FROM chain c JOIN auth.tbl_MenuItems p ON p.Id = c.ParentId WHERE p.IsVisible = 1 AND p.IsDeleted = 0)
INSERT INTO #before SELECT DISTINCT RoleId, Id FROM chain;

DECLARE @checks TABLE (n INT IDENTITY(1,1), prueba NVARCHAR(120), ok BIT, detalle NVARCHAR(200));
BEGIN TRAN;

-- 1) Grupos nuevos (sin URL, visibles)

DECLARE @g_th_personal INT, @g_th_contrat INT, @g_th_tiempo INT, @g_th_vac INT, @g_th_bien INT, @g_ad_seg INT, @g_ad_dir INT, @g_ad_sis INT, @g_ca_org INT, @g_ca_con INT, @g_ca_otr INT, @g_re_con INT, @g_re_sub INT, @g_re_aud INT, @g_em_tram INT, @g_em_apr INT, @g_gu_cfg INT, @g_gu_ope INT, @g_gu_vac INT;
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Personal', NULL, N'Users', 9, 10, 1, N'TalentoHumano', 0); SET @g_th_personal = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Contratación y movimientos', NULL, N'Handshake', 9, 20, 1, N'TalentoHumano', 0); SET @g_th_contrat = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Tiempo y asistencia', NULL, N'Clock', 9, 30, 1, N'TalentoHumano', 0); SET @g_th_tiempo = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Vacaciones', NULL, N'CalendarDays', 9, 40, 1, N'TalentoHumano', 0); SET @g_th_vac = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Bienestar y beneficios', NULL, N'UserCheck', 9, 50, 1, N'TalentoHumano', 0); SET @g_th_bien = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Seguridad y accesos', NULL, N'ShieldCheck', 2, 10, 1, N'Admin', 0); SET @g_ad_seg = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Directorio y Azure', NULL, N'Globe', 2, 20, 1, N'Admin', 0); SET @g_ad_dir = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Sistema', NULL, N'Cog', 2, 30, 1, N'Admin', 0); SET @g_ad_sis = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Organización y cargos', NULL, N'Building2', 8, 10, 1, N'Catalog', 0); SET @g_ca_org = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Contratos y trámites', NULL, N'FileText', 8, 20, 1, N'Catalog', 0); SET @g_ca_con = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Otros catálogos', NULL, N'Layers', 8, 30, 1, N'Catalog', 0); SET @g_ca_otr = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Contratos y acciones', NULL, N'FileText', 147, 10, 1, N'Reports', 0); SET @g_re_con = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Subsidios', NULL, N'DollarSign', 147, 20, 1, N'Reports', 0); SET @g_re_sub = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Auditoría y consultas', NULL, N'ShieldCheck', 6, 70, 1, N'Reports', 0); SET @g_re_aud = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Mis trámites', NULL, N'ClipboardList', 10, 20, 1, N'Empleado', 0); SET @g_em_tram = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Aprobaciones', NULL, N'CheckCircle', 10, 30, 1, N'Empleado', 0); SET @g_em_apr = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Configuración', NULL, N'Settings', 76, 20, 1, N'Guards', 0); SET @g_gu_cfg = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Operación', NULL, N'Shield', 76, 30, 1, N'Guards', 0); SET @g_gu_ope = SCOPE_IDENTITY();
INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted) VALUES (N'Vacaciones', NULL, N'CalendarDays', 76, 40, 1, N'Guards', 0); SET @g_gu_vac = SCOPE_IDENTITY();

-- 2) Padre y orden de cada ítem existente
CREATE TABLE #map (ItemId INT PRIMARY KEY, NewParentId INT NULL, Ord INT NOT NULL);
INSERT INTO #map (ItemId, NewParentId, Ord) VALUES (1,NULL,10), (7,NULL,20), (9,NULL,30), (10,NULL,40), (76,NULL,50), (172,NULL,60), (6,NULL,70), (8,NULL,80), (57,NULL,90), (126,NULL,100), (13,NULL,110), (136,NULL,120), (2,NULL,130), (12,NULL,140), (21,NULL,150), (31,NULL,160), (32,NULL,170), (26,9,60), (30,9,70), (22,@g_th_personal,10), (23,@g_th_personal,20), (24,@g_th_personal,30), (175,@g_th_personal,40), (113,@g_th_personal,50), (49,@g_th_contrat,10), (93,@g_th_contrat,20), (138,@g_th_contrat,30), (115,@g_th_contrat,40), (38,@g_th_tiempo,10), (27,@g_th_tiempo,20), (66,@g_th_tiempo,30), (161,@g_th_tiempo,40), (163,@g_th_tiempo,50), (152,@g_th_vac,10), (120,@g_th_vac,20), (121,@g_th_vac,30), (29,@g_th_bien,10), (149,@g_th_bien,20), (28,@g_th_bien,30), (168,2,40), (3,@g_ad_seg,10), (4,@g_ad_seg,20), (5,@g_ad_seg,30), (45,@g_ad_seg,40), (117,@g_ad_seg,50), (118,@g_ad_seg,60), (112,@g_ad_seg,70), (92,@g_ad_seg,80), (156,@g_ad_seg,90), (74,@g_ad_dir,10);
INSERT INTO #map (ItemId, NewParentId, Ord) VALUES (90,@g_ad_dir,20), (55,@g_ad_dir,30), (48,@g_ad_sis,10), (124,@g_ad_sis,20), (56,@g_ad_sis,30), (50,@g_ad_sis,40), (111,@g_ad_sis,50), (15,@g_ca_org,10), (67,@g_ca_org,20), (16,@g_ca_org,30), (91,@g_ca_org,40), (17,@g_ca_org,50), (14,@g_ca_con,10), (89,@g_ca_con,20), (119,@g_ca_con,30), (19,@g_ca_con,40), (122,@g_ca_otr,10), (123,@g_ca_otr,20), (125,@g_ca_otr,30), (20,@g_ca_otr,40), (18,@g_ca_otr,50), (94,@g_ca_otr,60), (144,6,10), (145,6,20), (146,6,30), (147,6,40), (104,6,50), (132,6,60), (11,144,10), (176,144,20), (68,144,30), (51,145,10), (54,145,20), (71,145,30), (72,145,40), (73,145,50), (153,145,60), (155,145,70), (157,145,80), (174,145,90), (52,146,10), (69,146,20), (70,146,30), (158,147,30), (101,147,40), (102,147,50), (103,147,60), (96,@g_re_con,10), (97,@g_re_con,20), (98,@g_re_con,30);
INSERT INTO #map (ItemId, NewParentId, Ord) VALUES (99,@g_re_con,40), (100,@g_re_con,50), (148,@g_re_sub,10), (150,@g_re_sub,20), (151,@g_re_sub,30), (154,@g_re_sub,40), (159,158,10), (160,158,20), (105,104,10), (106,104,20), (107,104,30), (108,104,40), (109,104,50), (133,132,10), (134,132,20), (135,132,30), (53,@g_re_aud,10), (179,@g_re_aud,20), (116,10,10), (42,10,40), (46,10,50), (39,10,60), (40,10,70), (43,10,80), (33,@g_em_tram,10), (35,@g_em_tram,20), (36,@g_em_tram,30), (37,@g_em_tram,40), (114,@g_em_tram,50), (41,@g_em_tram,60), (34,@g_em_apr,10), (65,@g_em_apr,20), (77,76,10), (95,@g_gu_cfg,10), (78,@g_gu_cfg,20), (80,@g_gu_cfg,30), (79,@g_gu_cfg,40), (85,@g_gu_cfg,50), (81,@g_gu_cfg,60), (86,@g_gu_cfg,70), (84,@g_gu_ope,10), (82,@g_gu_ope,20), (83,@g_gu_ope,30), (87,@g_gu_vac,10), (88,@g_gu_vac,20), (110,@g_gu_vac,30), (44,13,10), (58,57,10), (59,57,20), (60,57,30);
INSERT INTO #map (ItemId, NewParentId, Ord) VALUES (61,57,40), (62,57,50), (63,57,60), (64,57,70), (25,93,10), (142,93,20), (143,93,30), (141,93,40), (183,93,50), (127,126,10), (128,126,20), (129,126,30), (130,126,40), (131,126,50), (137,136,10), (75,138,10), (139,138,20), (140,138,30), (182,138,40), (170,172,10), (178,172,20);
IF (SELECT COUNT(*) FROM #map) <> (SELECT COUNT(*) FROM #snap WHERE IsDeleted = 0) OR EXISTS (SELECT 1 FROM #snap s WHERE s.IsDeleted = 0 AND NOT EXISTS (SELECT 1 FROM #map x WHERE x.ItemId = s.Id)) THROW 50313, 'El mapa no cubre exactamente los ítems no borrados.', 1;
UPDATE m SET m.ParentId = x.NewParentId, m.[Order] = x.Ord
FROM auth.tbl_MenuItems m JOIN #map x ON x.ItemId = m.Id;

-- 3) Menú resultante por rol, después del cambio
WITH base AS (
    SELECT r.RoleId, m.Id, m.ParentId FROM auth.tbl_RoleMenuItems r JOIN auth.tbl_MenuItems m ON m.Id = r.MenuItemId
    WHERE r.IsVisible = 1 AND m.IsVisible = 1 AND m.IsDeleted = 0),
chain AS (
    SELECT RoleId, Id, ParentId FROM base
    UNION ALL
    SELECT c.RoleId, p.Id, p.ParentId FROM chain c JOIN auth.tbl_MenuItems p ON p.Id = c.ParentId WHERE p.IsVisible = 1 AND p.IsDeleted = 0)
INSERT INTO #after SELECT DISTINCT RoleId, Id FROM chain;

-- 4) Verificaciones
INSERT @checks (prueba, ok, detalle) SELECT N'Ninguna fila borrada y 19 grupos nuevos', CASE WHEN (SELECT COUNT(*) FROM auth.tbl_MenuItems) = (SELECT COUNT(*) FROM #snap) + 19 THEN 1 ELSE 0 END,
    CONCAT((SELECT COUNT(*) FROM #snap), N' antes, ', (SELECT COUNT(*) FROM auth.tbl_MenuItems), N' después');
INSERT @checks (prueba, ok, detalle) SELECT N'Asignaciones de roles intactas', CASE WHEN (SELECT COUNT(*) FROM auth.tbl_RoleMenuItems) = @rolesRows THEN 1 ELSE 0 END, CONCAT(@rolesRows, N' filas');
INSERT @checks (prueba, ok, detalle) SELECT N'Nombre, URL, icono, visibilidad y módulo sin cambios (solo ParentId/Order)',
    CASE WHEN NOT EXISTS (SELECT Id, Name, Url, Icon, IsVisible, ModuleName, IsDeleted FROM #snap
                          EXCEPT SELECT Id, Name, Url, Icon, IsVisible, ModuleName, IsDeleted FROM auth.tbl_MenuItems) THEN 1 ELSE 0 END, N'';
INSERT @checks (prueba, ok, detalle)
SELECT N'Páginas visibles por rol: ninguna se pierde ni aparece una nueva',
    CASE WHEN NOT EXISTS (SELECT b.RoleId, b.ItemId FROM #before b JOIN #snap s ON s.Id = b.ItemId WHERE s.Url IS NOT NULL AND s.Url <> N''
                          EXCEPT SELECT a.RoleId, a.ItemId FROM #after a JOIN auth.tbl_MenuItems m ON m.Id = a.ItemId WHERE m.Url IS NOT NULL AND m.Url <> N'')
          AND NOT EXISTS (SELECT a.RoleId, a.ItemId FROM #after a JOIN auth.tbl_MenuItems m ON m.Id = a.ItemId WHERE m.Url IS NOT NULL AND m.Url <> N''
                          EXCEPT SELECT b.RoleId, b.ItemId FROM #before b JOIN #snap s ON s.Id = b.ItemId WHERE s.Url IS NOT NULL AND s.Url <> N'') THEN 1 ELSE 0 END,
    CONCAT((SELECT COUNT(DISTINCT RoleId) FROM #before), N' roles; ', (SELECT COUNT(*) FROM #before b JOIN #snap s ON s.Id = b.ItemId WHERE s.Url IS NOT NULL AND s.Url <> N''), N' páginas-rol antes, ',
           (SELECT COUNT(*) FROM #after a JOIN auth.tbl_MenuItems m ON m.Id = a.ItemId WHERE m.Url IS NOT NULL AND m.Url <> N''), N' después');
INSERT @checks (prueba, ok, detalle)
SELECT N'Ningún ítem del menú queda sin su padre', CASE WHEN NOT EXISTS (
    SELECT 1 FROM #after a JOIN auth.tbl_MenuItems m ON m.Id = a.ItemId
    WHERE m.ParentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #after p WHERE p.RoleId = a.RoleId AND p.ItemId = m.ParentId)) THEN 1 ELSE 0 END, N'';
INSERT @checks (prueba, ok, detalle)
SELECT N'Órdenes únicos entre hermanos', CASE WHEN NOT EXISTS (
    SELECT 1 FROM auth.tbl_MenuItems WHERE IsDeleted = 0 GROUP BY ISNULL(ParentId, -1), [Order] HAVING COUNT(*) > 1) THEN 1 ELSE 0 END, N'';
INSERT @checks (prueba, ok, detalle)
SELECT N'Todos los ítems llegan a una raíz (sin ciclos ni padres inexistentes)', CASE WHEN
    (SELECT COUNT(*) FROM auth.tbl_MenuItems WHERE IsDeleted = 0) = (
        SELECT COUNT(*) FROM (
            SELECT Id FROM auth.tbl_MenuItems WHERE IsDeleted = 0 AND ParentId IS NULL
            UNION ALL
            SELECT m.Id FROM auth.tbl_MenuItems m WHERE m.IsDeleted = 0 AND m.ParentId IS NOT NULL AND EXISTS (SELECT 1 FROM auth.tbl_MenuItems p WHERE p.Id = m.ParentId AND p.IsDeleted = 0)
        ) z)
    AND NOT EXISTS (SELECT 1 FROM auth.tbl_MenuItems m WHERE m.IsDeleted = 0 AND m.ParentId = m.Id) THEN 1 ELSE 0 END, N'';
INSERT @checks (prueba, ok, detalle)
SELECT N'Ningún grupo nuevo queda vacío', CASE WHEN NOT EXISTS (
    SELECT 1 FROM auth.tbl_MenuItems g WHERE g.Id > (SELECT MAX(Id) FROM #snap) AND NOT EXISTS (SELECT 1 FROM auth.tbl_MenuItems c WHERE c.ParentId = g.Id AND c.IsDeleted = 0)) THEN 1 ELSE 0 END, N'';

SELECT n, prueba, CASE ok WHEN 1 THEN N'OK' ELSE N'FALLA' END AS resultado, detalle FROM @checks ORDER BY n;
IF EXISTS (SELECT 1 FROM @checks WHERE ok = 0) BEGIN ROLLBACK TRAN; THROW 50314, 'Alguna verificación falló: se revirtió todo.', 1; END

-- 5) Resultado por rol: páginas visibles antes / después
SELECT ro.Name AS rol, (SELECT COUNT(*) FROM #before b JOIN #snap s ON s.Id = b.ItemId WHERE b.RoleId = ro.Id AND s.Url IS NOT NULL AND s.Url <> N'') AS paginas_antes,
       (SELECT COUNT(*) FROM #after a JOIN auth.tbl_MenuItems m ON m.Id = a.ItemId WHERE a.RoleId = ro.Id AND m.Url IS NOT NULL AND m.Url <> N'') AS paginas_despues
FROM auth.tbl_Roles ro WHERE ro.Id IN (SELECT RoleId FROM #before) ORDER BY ro.Name;

IF '$(Commit)' = '1' BEGIN COMMIT TRAN; PRINT 'APLICADO.'; END
ELSE BEGIN ROLLBACK TRAN; PRINT 'SIMULACIÓN: todo revertido (Commit=0).'; END
