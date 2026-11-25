/* =====================================================================
   65_occupational_model_02_data.sql
   Migración de datos Cargo -> Escala (Grupo x Grado + Nivel).
   Uso:  sqlcmd ... -f 65001 -v Commit=0 -i 65_occupational_model_02_data.sql   (simulación: revierte)
         sqlcmd ... -f 65001 -v Commit=1 -i 65_occupational_model_02_data.sql   (aplica)
   - Una sola transacción; si alguna verificación falla, se revierte todo.
   - Compara contra las fotos de 65_occupational_model_00_backup.sql: el sueldo que resuelve
     fn_ResolveEmployeeRate solo puede cambiar para los empleados de los cargos reclasificados
     cuya escala tiene otro RMU referencial (cargos 17 y 135).
   - Idempotente: si la migración ya se aplicó, no hace nada.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit BIT = $(Commit);

IF OBJECT_ID(N'HR.bak_Manifest_20261125', N'U') IS NULL
    THROW 50001, 'Falta el respaldo: ejecutar primero 65_occupational_model_00_backup.sql', 1;
IF EXISTS (SELECT 1 FROM HR.bak_Manifest_20261125 WHERE SourceRows <> BackupRows)
    THROW 50002, 'El manifiesto de respaldo tiene diferencias; no se migra.', 1;
IF COL_LENGTH(N'HR.tbl_Occupational_Groups', N'Level') IS NULL
    THROW 50003, 'Falta la estructura: ejecutar 65_occupational_model_01_structure.sql', 1;

IF EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = N'OCCUPATIONAL_GROUP_FAMILY' AND Name = N'Autoridades Académicas')
BEGIN
    PRINT 'La migración ya fue aplicada; no se hace nada.';
    RETURN;
END;

-- Nada debe haber cambiado en cargos desde el respaldo
IF EXISTS (SELECT JobID, GroupID FROM HR.tbl_jobs EXCEPT SELECT JobID, GroupID FROM HR.tbl_jobs_bak_20261125)
   OR EXISTS (SELECT JobID, GroupID FROM HR.tbl_jobs_bak_20261125 EXCEPT SELECT JobID, GroupID FROM HR.tbl_jobs)
    THROW 50004, 'Los cargos cambiaron desde el respaldo; repetir el respaldo antes de migrar.', 1;

BEGIN TRAN;

/* ---- 1) Grupos (familias) en ref_Types ------------------------------------------- */
DECLARE @fam TABLE (Name NVARCHAR(200) PRIMARY KEY, Descr NVARCHAR(400), Prefix NVARCHAR(20) NULL, Sort INT);
INSERT INTO @fam (Name, Descr, Prefix, Sort) VALUES
 (N'Autoridades Académicas',            N'Rector, vicerrectores, decanos, subdecanos y coordinadores', N'Nivel ', 10),
 (N'Personal Académico Auxiliar',       N'Profesores titulares auxiliares',                              N'Nivel ', 20),
 (N'Personal Académico Agregado',       N'Profesores titulares agregados',                               N'Nivel ', 30),
 (N'Personal Académico Principal',      N'Profesores titulares principales',                             N'Nivel ', 40),
 (N'Profesor Universitario',            N'Profesores ocasionales, honorarios e invitados',               NULL,      50),
 (N'Personal de Apoyo (Universidades)', N'Técnicos docentes, de laboratorio y de investigación',         NULL,      60),
 (N'UEP Nivel Directivo B1',            N'Nivel directivo UEP B1',                                       NULL,      70),
 (N'Servidor Público',                  N'Escala LOSEP de servidores públicos',                          NULL,      80),
 (N'Servidor Público de Apoyo',         N'Escala LOSEP de servidores públicos de apoyo',                 NULL,      90),
 (N'Servidor Público de Servicios',     N'Escala LOSEP de servidores públicos de servicios',             NULL,     100),
 (N'Nivel Jerárquico Superior',         N'Escala de nivel jerárquico superior',                          NULL,     110),
 (N'Código de Trabajo',                 N'Personal obrero (grados pendientes de definir por RRHH)',      NULL,     120);

INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, CreatedAt, SortOrder, Metadata)
SELECT N'OCCUPATIONAL_GROUP_FAMILY', f.Name, f.Descr, 1, SYSDATETIME(), f.Sort,
       CASE WHEN f.Prefix IS NULL THEN NULL ELSE N'{"levelPrefix":"' + f.Prefix + N'"}' END
FROM @fam f
WHERE NOT EXISTS (SELECT 1 FROM HR.ref_Types r WHERE r.Category = N'OCCUPATIONAL_GROUP_FAMILY' AND r.Name = f.Name);

-- La familia anterior "Personal Académico Titular" queda inactiva (se reemplaza por Auxiliar/Agregado/Principal)
UPDATE HR.ref_Types SET IsActive = 0, UpdatedAt = SYSDATETIME()
WHERE Category = N'OCCUPATIONAL_GROUP_FAMILY' AND Name = N'Personal Académico Titular';

/* ---- 2) Mapeo de las escalas existentes (id -> grupo, nivel, grado) ---------------- */
DECLARE @map TABLE (GroupID INT PRIMARY KEY, Family NVARCHAR(200) NOT NULL, Lvl INT NULL, DegreeID INT NULL);
INSERT INTO @map (GroupID, Family, Lvl, DegreeID) VALUES
 -- Servidor Público de Servicios / Apoyo / Servidor Público (nivel = número del nombre; el grado no cambia)
 (1,  N'Servidor Público de Servicios', 1, NULL), (2,  N'Servidor Público de Servicios', 2, NULL),
 (3,  N'Servidor Público de Apoyo', 1, NULL), (4,  N'Servidor Público de Apoyo', 2, NULL),
 (5,  N'Servidor Público de Apoyo', 3, NULL), (6,  N'Servidor Público de Apoyo', 4, NULL),
 (7,  N'Servidor Público', 1, NULL),  (8,  N'Servidor Público', 2, NULL),  (9,  N'Servidor Público', 3, NULL),
 (10, N'Servidor Público', 4, NULL),  (11, N'Servidor Público', 5, NULL),  (12, N'Servidor Público', 6, NULL),
 (13, N'Servidor Público', 7, NULL),  (14, N'Servidor Público', 8, NULL),  (15, N'Servidor Público', 9, NULL),
 (16, N'Servidor Público', 10, NULL), (17, N'Servidor Público', 11, NULL), (18, N'Servidor Público', 12, NULL),
 (19, N'Servidor Público', 13, NULL), (20, N'Servidor Público', 14, NULL),
 -- Nivel Jerárquico Superior (la escala 41 "NJS 5 / 3.247" se fusiona en NJS 2 más abajo)
 (40, N'Nivel Jerárquico Superior', 1, NULL), (44, N'Nivel Jerárquico Superior', 2, NULL),
 (29, N'Nivel Jerárquico Superior', 3, NULL), (33, N'Nivel Jerárquico Superior', 4, NULL),
 (32, N'Nivel Jerárquico Superior', 5, NULL),
 -- Personal académico: grado 1 a 8 (catálogo "Grado N"), nivel dentro de cada grupo
 (59, N'Personal Académico Auxiliar', 1, 1), (60, N'Personal Académico Auxiliar', 2, 2),
 (61, N'Personal Académico Agregado', 1, 3), (62, N'Personal Académico Agregado', 2, 4), (63, N'Personal Académico Agregado', 3, 5),
 (64, N'Personal Académico Principal', 1, 6), (65, N'Personal Académico Principal', 2, 7), (66, N'Personal Académico Principal', 3, 8),
 -- Autoridades: el Rector ya existe (escala 50) con grado de relleno
 (50, N'Autoridades Académicas', 4, 4),
 -- UEP Nivel Directivo B1: se conserva la 68
 (68, N'UEP Nivel Directivo B1', NULL, 1);

-- Los grados que estaban inactivos y vuelven a usarse (NJS) se reactivan
UPDATE d SET IsActive = 1, UpdatedAt = SYSDATETIME()
FROM HR.tbl_Degrees d
WHERE d.IsActive = 0 AND d.DegreeID IN (SELECT og.DegreeID FROM HR.tbl_Occupational_Groups og JOIN @map m ON m.GroupID = og.GroupID WHERE m.DegreeID IS NULL);

UPDATE og SET
    FamilyTypeId = f.TypeID,
    [Level]      = m.Lvl,
    DegreeID     = COALESCE(m.DegreeID, og.DegreeID),
    UepScaleTypeId = CASE WHEN og.GroupID = 68 THEN (SELECT TypeID FROM HR.ref_Types WHERE Category = N'UEP_SCALE_TYPE' AND Name = N'UEP Nivel Directivo B1') ELSE og.UepScaleTypeId END,
    Description  = f.Name + CASE WHEN m.Lvl IS NULL THEN N'' ELSE N' ' + COALESCE(JSON_VALUE(f.Metadata, '$.levelPrefix'), N'') + CAST(m.Lvl AS NVARCHAR(10)) END,
    IsActive     = 1,
    UpdatedAt    = SYSDATETIME()
FROM HR.tbl_Occupational_Groups og
JOIN @map m ON m.GroupID = og.GroupID
JOIN HR.ref_Types f ON f.Category = N'OCCUPATIONAL_GROUP_FAMILY' AND f.Name = m.Family;

/* ---- 3) Escalas nuevas ------------------------------------------------------------ */
INSERT INTO HR.tbl_Occupational_Groups (Description, RMU, DegreeID, IsActive, CreatedAt, FamilyTypeId, [Level])
SELECT f.Name + CASE WHEN n.Lvl IS NULL THEN N'' ELSE N' ' + COALESCE(JSON_VALUE(f.Metadata, '$.levelPrefix'), N'') + CAST(n.Lvl AS NVARCHAR(10)) END,
       NULL, n.DegreeID, 1, SYSDATETIME(), f.TypeID, n.Lvl
FROM (VALUES (N'Autoridades Académicas', 1, 1), (N'Autoridades Académicas', 2, 2), (N'Autoridades Académicas', 3, 3),
             (N'Profesor Universitario', NULL, 1), (N'Personal de Apoyo (Universidades)', NULL, 1)) AS n (Fam, Lvl, DegreeID)
JOIN HR.ref_Types f ON f.Category = N'OCCUPATIONAL_GROUP_FAMILY' AND f.Name = n.Fam
WHERE NOT EXISTS (SELECT 1 FROM HR.tbl_Occupational_Groups og WHERE og.FamilyTypeId = f.TypeID AND og.DegreeID = n.DegreeID);

/* ---- 4) Cargos: reclasificación -------------------------------------------------- */
DECLARE @jobmap TABLE (JobID INT PRIMARY KEY, TargetId INT NULL, TargetName NVARCHAR(400) NULL, Motivo NVARCHAR(200));
INSERT INTO @jobmap (JobID, TargetId, TargetName, Motivo) VALUES
 -- Fusión 24 -> 68 (UEP Nivel Directivo B1)
 (114, 68, NULL, N'Fusión Directivo 1 -> UEP Nivel Directivo B1'), (119, 68, NULL, N'Fusión Directivo 1 -> UEP Nivel Directivo B1'),
 (120, 68, NULL, N'Fusión Directivo 1 -> UEP Nivel Directivo B1'), (194, 68, NULL, N'Fusión Directivo 1 -> UEP Nivel Directivo B1'),
 (199, 68, NULL, N'Fusión Directivo 1 -> UEP Nivel Directivo B1'), (203, 68, NULL, N'Fusión UEP B1 duplicada'),
 (196, 68, NULL, N'RRHH: Director Administrativo = UEP Nivel Directivo B1, grado 1'),
 -- NJS: la escala 41 (3.247) corresponde a NJS 2; el resto se alinea por el número del nombre
 (124, 44, NULL, N'Fusión NJS 5 (3.247) -> NJS 2'), (142, 44, NULL, N'Fusión NJS 5 (3.247) -> NJS 2'), (147, 44, NULL, N'Fusión NJS 5 (3.247) -> NJS 2'),
 (123, 33, NULL, N'Asesor 4 -> NJS 4'), (127, 29, NULL, N'Asesor 3 -> NJS 3'), (141, 32, NULL, N'Asesor 5 -> NJS 5'),
 -- Código de Trabajo y servidor público
 (135, 46, NULL, N'Escala CT2 inactiva -> Código de Trabajo Nivel 2'),
 (17,   7, NULL, N'Escala de relleno (UEP B3, RMU 0) -> Servidor Público 1 (RMU ref. 817)'),
 -- RRHH: autoridades
 (240, NULL, N'Autoridades Académicas Nivel 3', N'RRHH: vicerrector, grado 3'),
 (192, NULL, N'Autoridades Académicas Nivel 2', N'RRHH: decano, grado 2'),
 (234, NULL, N'Autoridades Académicas Nivel 1', N'RRHH: subdecano, grado 1'),
 (184, NULL, N'Autoridades Académicas Nivel 1', N'RRHH: coordinador de carrera, grado 1'),
 (185, NULL, N'Autoridades Académicas Nivel 1', N'RRHH: coordinador de unidad, grado 1'),
 (186, NULL, N'Autoridades Académicas Nivel 1', N'RRHH: coordinador de unidad, grado 1'),
 (187, NULL, N'Autoridades Académicas Nivel 1', N'RRHH: coordinador de unidad, grado 1'),
 (188, NULL, N'Autoridades Académicas Nivel 1', N'RRHH: coordinador de unidad, grado 1'),
 (191, NULL, N'Autoridades Académicas Nivel 1', N'RRHH: coordinador de unidad, grado 1'),
 -- RRHH: docentes y técnicos
 (117, NULL, N'Profesor Universitario', N'RRHH: profesor ocasional = Profesor Universitario, grado 1'),
 (118, NULL, N'Profesor Universitario', N'RRHH: profesor (inactivo)'),
 (238, NULL, N'Personal de Apoyo (Universidades)', N'RRHH: técnico docente'),
 (237, NULL, N'Personal de Apoyo (Universidades)', N'RRHH: técnico de laboratorio'),
 (236, NULL, N'Personal de Apoyo (Universidades)', N'RRHH: técnico de investigación');

UPDATE j SET GroupID = COALESCE(m.TargetId, og.GroupID), UpdatedAt = SYSDATETIME()
FROM HR.tbl_jobs j
JOIN @jobmap m ON m.JobID = j.JobID
LEFT JOIN HR.tbl_Occupational_Groups og ON m.TargetId IS NULL AND og.Description = m.TargetName;

IF EXISTS (SELECT 1 FROM @jobmap m JOIN HR.tbl_jobs j ON j.JobID = m.JobID WHERE j.GroupID IS NULL)
    THROW 50010, 'Algún cargo reclasificado quedó sin escala (nombre de destino no encontrado).', 1;

/* ---- 5) Escalas que quedan sin uso -> inactivas (no se elimina nada) -------------- */
UPDATE HR.tbl_Occupational_Groups SET IsActive = 0, UpdatedAt = SYSDATETIME(), FamilyTypeId = NULL, [Level] = NULL
WHERE GroupID IN (24, 41, 67);                                  -- fusionadas/relleno
UPDATE HR.tbl_Occupational_Groups SET IsActive = 0, UpdatedAt = SYSDATETIME() WHERE GroupID IN (21, 45); -- residuos
UPDATE HR.tbl_Occupational_Groups SET IsActive = 1, UpdatedAt = SYSDATETIME() WHERE GroupID = 46;         -- CT Nivel 2 activa
-- Grados que dejan de usarse: duplicado "GRADO 1" y "Grado escalafonario 1..8"
UPDATE d SET IsActive = 0, UpdatedAt = SYSDATETIME()
FROM HR.tbl_Degrees d
WHERE d.DegreeID IN (27, 49, 50, 51, 52, 53, 54, 55, 56)
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_Occupational_Groups og WHERE og.DegreeID = d.DegreeID AND og.IsActive = 1);

/* ---- 5b) vw_EmployeeAcademicGroup: une por grupo + nivel, ya no por el texto del nombre ------ */
-- Antes: g.Description = 'Personal Académico Titular - <Categoría> - Nivel <n>'. Con los nombres nuevos esa igualdad no existiría.
-- Mismas columnas y mismo significado: cada docente conserva su escala (se verifica contra la foto más abajo).
EXEC (N'
CREATE OR ALTER VIEW HR.vw_EmployeeAcademicGroup AS
SELECT
    ts.EmployeeID,
    ts.TeacherStructureID,
    ts.LadderID,
    l.Name          AS LadderName,
    g.GroupID,
    g.Description   AS GroupDescription,
    g.DegreeID,
    d.Description   AS Degree,
    g.RMU           AS GroupRMU,
    g.IsActive      AS GroupIsActive
FROM HR.tbl_TeacherStructure ts
JOIN HR.tbl_AcademicLadder l ON l.LadderID = ts.LadderID
JOIN HR.ref_Types rc ON rc.TypeID = l.CategoryTypeID AND rc.Category = N''ACADEMIC_CATEGORY''
JOIN HR.ref_Types rl ON rl.TypeID = l.LevelTypeID    AND rl.Category = N''ACADEMIC_LEVEL''
JOIN HR.ref_Types fam ON fam.Category = N''OCCUPATIONAL_GROUP_FAMILY''
 AND fam.Name = N''Personal Académico ''
       + CASE rc.Name WHEN N''TITULAR_AUXILIAR'' THEN N''Auxiliar'' WHEN N''TITULAR_AGREGADO'' THEN N''Agregado'' WHEN N''TITULAR_PRINCIPAL'' THEN N''Principal'' END
JOIN HR.tbl_Occupational_Groups g
  ON g.FamilyTypeId = fam.TypeID
 AND g.[Level] = TRY_CAST(RIGHT(rl.Name, 1) AS INT)
LEFT JOIN HR.tbl_Degrees d ON d.DegreeID = g.DegreeID
WHERE ts.IsActive = 1;');

/* ---- 6) Verificaciones ------------------------------------------------------------ */
IF (SELECT COUNT(*) FROM HR.tbl_jobs) <> (SELECT COUNT(*) FROM HR.tbl_jobs_bak_20261125)
    THROW 50020, 'Cambió el número de cargos.', 1;

IF EXISTS (SELECT 1 FROM HR.tbl_jobs j JOIN HR.tbl_Occupational_Groups og ON og.GroupID = j.GroupID
           WHERE j.IsActive = 1 AND og.IsActive = 0)
    THROW 50021, 'Quedan cargos activos bajo una escala inactiva.', 1;

IF EXISTS (SELECT 1 FROM HR.tbl_Occupational_Groups og JOIN HR.tbl_Degrees d ON d.DegreeID = og.DegreeID
           WHERE og.IsActive = 1 AND d.IsActive = 0)
    THROW 50022, 'Hay escalas activas con un grado inactivo.', 1;

-- Los únicos cargos cuyo grupo cambió son los del mapeo
IF EXISTS (SELECT 1 FROM HR.tbl_jobs j JOIN HR.tbl_jobs_bak_20261125 b ON b.JobID = j.JobID
           WHERE ISNULL(j.GroupID, -1) <> ISNULL(b.GroupID, -1) AND j.JobID NOT IN (SELECT JobID FROM @jobmap))
    THROW 50023, 'Cambió el grupo de un cargo fuera del mapeo.', 1;

-- Cada docente conserva la misma escala que resolvía la vista antes
IF EXISTS (SELECT EmployeeID, TeacherStructureID, LadderID, GroupID, GroupRMU FROM HR.snap_vw_EmployeeAcademicGroup_bak_20261125
           EXCEPT SELECT EmployeeID, TeacherStructureID, LadderID, GroupID, GroupRMU FROM HR.vw_EmployeeAcademicGroup)
   OR EXISTS (SELECT EmployeeID, TeacherStructureID, LadderID, GroupID, GroupRMU FROM HR.vw_EmployeeAcademicGroup
           EXCEPT SELECT EmployeeID, TeacherStructureID, LadderID, GroupID, GroupRMU FROM HR.snap_vw_EmployeeAcademicGroup_bak_20261125)
    THROW 50026, 'vw_EmployeeAcademicGroup no resuelve la misma escala por docente que antes.', 1;

-- Sueldo resuelto: solo pueden diferir los empleados de cargos cuyo RMU referencial cambió (17 y 135)
DECLARE @hoy DATE = (SELECT TOP 1 AsOfDate FROM HR.snap_fn_ResolveEmployeeRate_bak_20261125);
-- EXCEPT trata los NULL como iguales (LaborRegimeID puede venir nulo)
SELECT EmployeeID, LaborRegimeID, RMU, HourRate, SourceType, IsPrincipal
INTO #rateNow FROM HR.fn_ResolveEmployeeRate(@hoy, NULL);
SELECT EmployeeID, LaborRegimeID, RMU, HourRate, SourceType, IsPrincipal
INTO #rateOld FROM HR.snap_fn_ResolveEmployeeRate_bak_20261125;

SELECT x.EmployeeID, x.LaborRegimeID, x.RmuAnterior, x.RmuNuevo
INTO #diffRate
FROM (
    SELECT o.EmployeeID, o.LaborRegimeID, o.RMU AS RmuAnterior, CAST(NULL AS DECIMAL(18,2)) AS RmuNuevo
    FROM (SELECT * FROM #rateOld EXCEPT SELECT * FROM #rateNow) o
    UNION ALL
    SELECT n.EmployeeID, n.LaborRegimeID, CAST(NULL AS DECIMAL(18,2)), n.RMU
    FROM (SELECT * FROM #rateNow EXCEPT SELECT * FROM #rateOld) n
) x;
DROP TABLE #rateNow;
DROP TABLE #rateOld;

DECLARE @filasNuevas INT = (SELECT COUNT(*) FROM HR.fn_ResolveEmployeeRate(@hoy, NULL));
DECLARE @filasAntes  INT = (SELECT COUNT(*) FROM HR.snap_fn_ResolveEmployeeRate_bak_20261125);
IF @filasNuevas <> @filasAntes THROW 50024, 'Cambió el número de filas que resuelve fn_ResolveEmployeeRate.', 1;

IF EXISTS (SELECT 1 FROM #diffRate d JOIN HR.tbl_Employees e ON e.EmployeeID = d.EmployeeID WHERE e.JobID NOT IN (17, 135))
BEGIN
    SELECT TOP 40 d.EmployeeID, e.JobID, d.LaborRegimeID, d.RmuAnterior, d.RmuNuevo
    FROM #diffRate d JOIN HR.tbl_Employees e ON e.EmployeeID = d.EmployeeID WHERE e.JobID NOT IN (17, 135);
    THROW 50025, 'Cambió el sueldo resuelto de empleados fuera de los cargos 17 y 135.', 1;
END;

/* ---- 7) Resumen ------------------------------------------------------------------- */
SELECT 'cargos reclasificados' AS Concepto, COUNT(*) AS Valor FROM @jobmap
UNION ALL SELECT 'empleados con sueldo resuelto distinto', COUNT(*) FROM #diffRate
UNION ALL SELECT 'escalas activas con grupo', COUNT(*) FROM HR.tbl_Occupational_Groups WHERE IsActive = 1 AND FamilyTypeId IS NOT NULL
UNION ALL SELECT 'escalas activas sin grupo (Código de Trabajo pendiente)', COUNT(*) FROM HR.tbl_Occupational_Groups WHERE IsActive = 1 AND FamilyTypeId IS NULL
UNION ALL SELECT 'cargos activos sin escala (pendientes de RRHH)', COUNT(*) FROM HR.tbl_jobs WHERE IsActive = 1 AND GroupID IS NULL;

SELECT TOP 20 d.EmployeeID, e.JobID, d.RmuAnterior, d.RmuNuevo FROM #diffRate d JOIN HR.tbl_Employees e ON e.EmployeeID = d.EmployeeID;
DROP TABLE #diffRate;

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT 'MIGRACIÓN APLICADA (COMMIT).';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT 'SIMULACIÓN: todo se revirtió (ROLLBACK).';
END;
