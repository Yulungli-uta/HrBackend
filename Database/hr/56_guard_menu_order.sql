-- Guardias: orden del submenú según la secuencia de trabajo del usuario (configuración -> operación -> vacaciones)
-- y dos nombres con ortografía/redacción corregida. Solo cambia auth.tbl_MenuItems (menú); no toca permisos ni rutas.
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @orden TABLE (Id int PRIMARY KEY, NuevoOrden int NOT NULL);
INSERT INTO @orden (Id, NuevoOrden) VALUES
    (77, 1),   -- Dashboard
    (95, 2),   -- Parámetros de Guardias
    (78, 3),   -- Ubicaciones
    (80, 4),   -- Patrones de Rotación
    (79, 5),   -- Grupos de Rotación
    (85, 6),   -- Asignación de Ubicaciones
    (81, 7),   -- Requerimientos de Cobertura
    (86, 8),   -- Reglas especiales
    (84, 9),   -- Disponibilidad
    (82, 10),  -- Planificación de Turnos
    (83, 11),  -- Aprobación Cambios de Turno
    (87, 12),  -- Planificación Vacaciones
    (88, 13),  -- Solicitud de Vacaciones
    (110, 14); -- Aprobación Vacaciones

UPDATE m SET m.[Order] = o.NuevoOrden
FROM auth.tbl_MenuItems m JOIN @orden o ON o.Id = m.Id
WHERE m.ParentId = 76 AND m.IsDeleted = 0;
IF @@ROWCOUNT <> 14 BEGIN ROLLBACK; THROW 50000, 'No coincidieron las 14 opciones del submenú de Guardias', 1; END

UPDATE auth.tbl_MenuItems SET Name = N'Parámetros de Guardias' WHERE Id = 95 AND ParentId = 76 AND IsDeleted = 0;
IF @@ROWCOUNT <> 1 BEGIN ROLLBACK; THROW 50000, 'No se encontró la opción Parámetros', 1; END
UPDATE auth.tbl_MenuItems SET Name = N'Solicitud de Vacaciones' WHERE Id = 88 AND ParentId = 76 AND IsDeleted = 0;
IF @@ROWCOUNT <> 1 BEGIN ROLLBACK; THROW 50000, 'No se encontró la opción Solicitud', 1; END

COMMIT;
SELECT Id, Name, Url, [Order] FROM auth.tbl_MenuItems WHERE ParentId = 76 AND IsDeleted = 0 ORDER BY [Order];
