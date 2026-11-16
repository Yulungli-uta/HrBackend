-- ============================================================================
-- 44_academic_titular_occupational_groups.sql
-- Grupos ocupacionales del personal académico titular, uno por grado escalafonario (1 a 8):
--   Auxiliar 1 y 2 = grados 1 y 2 | Agregado 1, 2 y 3 = grados 3, 4 y 5 | Principal 1, 2 y 3 = grados 6, 7 y 8.
-- Cada grupo lleva su propio grado ("Grado escalafonario N", distinto de los Grado 1..20 de la escala LOSEP) y el RMU de
-- referencia del cargo base de ese nivel (HR.tbl_Jobs.ReferenceSalary). Principal 3 no tiene cargo ni sueldo de referencia:
-- queda inactivo y con RMU 0 hasta que RRHH confirme el valor oficial.
-- Familia explícita: columna FamilyTypeId (ref_Types OCCUPATIONAL_GROUP_FAMILY = "Personal Académico Titular") con índice único (familia, grado).
-- Ata el grupo a los cargos de titulares (la categoría/nivel ya la da AcademicLadderId; el grupo aporta grupo, grado y RMU
-- al documento de la acción). Idempotente: se identifica por nombre y solo completa GroupId cuando está vacío.
-- ============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1) Grados escalafonarios 1..8
INSERT INTO HR.tbl_Degrees (Description, IsActive, CreatedAt)
SELECT N'Grado escalafonario ' + CAST(n.N AS nvarchar(2)), 1, SYSDATETIME()
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8)) n(N)
WHERE NOT EXISTS (SELECT 1 FROM HR.tbl_Degrees d WHERE d.Description = N'Grado escalafonario ' + CAST(n.N AS nvarchar(2)));
GO

-- 2) Grupos ocupacionales (uno por grado escalafonario)
INSERT INTO HR.tbl_Occupational_Groups (Description, RMU, DegreeID, IsActive, CreatedAt)
SELECT v.Name, v.Rmu, d.DegreeID, v.IsActive, SYSDATETIME()
FROM (VALUES
    (1, N'Personal Académico Titular - Auxiliar - Nivel 1',  CAST(2000.00 AS decimal(18,2)), CAST(1 AS bit)),
    (2, N'Personal Académico Titular - Auxiliar - Nivel 2',  CAST(2231.57 AS decimal(18,2)), CAST(1 AS bit)),
    (3, N'Personal Académico Titular - Agregado - Nivel 1',  CAST(3099.90 AS decimal(18,2)), CAST(1 AS bit)),
    (4, N'Personal Académico Titular - Agregado - Nivel 2',  CAST(3500.00 AS decimal(18,2)), CAST(1 AS bit)),
    (5, N'Personal Académico Titular - Agregado - Nivel 3',  CAST(4000.00 AS decimal(18,2)), CAST(1 AS bit)),
    (6, N'Personal Académico Titular - Principal - Nivel 1', CAST(4306.13 AS decimal(18,2)), CAST(1 AS bit)),
    (7, N'Personal Académico Titular - Principal - Nivel 2', CAST(4640.11 AS decimal(18,2)), CAST(1 AS bit)),
    (8, N'Personal Académico Titular - Principal - Nivel 3', CAST(0.00    AS decimal(18,2)), CAST(0 AS bit))
) v(Grade, Name, Rmu, IsActive)
JOIN HR.tbl_Degrees d ON d.Description = N'Grado escalafonario ' + CAST(v.Grade AS nvarchar(2))
WHERE NOT EXISTS (SELECT 1 FROM HR.tbl_Occupational_Groups g WHERE g.Description = v.Name);
GO

-- 2b) Familia del grupo: "Personal Académico Titular" como dato explícito (antes solo era el prefijo del nombre).
--     FamilyTypeId (opcional) apunta a ref_Types (Category = 'OCCUPATIONAL_GROUP_FAMILY'). Los grupos LOSEP quedan con la familia vacía.
--     El índice único filtrado impide dos grupos con el mismo grado dentro de una misma familia.
IF COL_LENGTH('HR.tbl_Occupational_Groups', 'FamilyTypeId') IS NULL
    ALTER TABLE HR.tbl_Occupational_Groups ADD FamilyTypeId INT NULL;
GO

INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, CreatedAt, SortOrder)
SELECT N'OCCUPATIONAL_GROUP_FAMILY', N'Personal Académico Titular',
       N'Docentes titulares con escalafón: Auxiliar, Agregado y Principal (grados escalafonarios 1 a 8)', 1, SYSDATETIME(), 1
WHERE NOT EXISTS (SELECT 1 FROM HR.ref_Types r WHERE r.Category = N'OCCUPATIONAL_GROUP_FAMILY' AND r.Name = N'Personal Académico Titular');
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OccupationalGroups_FamilyType')
    ALTER TABLE HR.tbl_Occupational_Groups
        ADD CONSTRAINT FK_OccupationalGroups_FamilyType FOREIGN KEY (FamilyTypeId) REFERENCES HR.ref_Types (TypeID);
GO

UPDATE g
   SET g.FamilyTypeId = t.TypeID, g.UpdatedAt = SYSDATETIME()
  FROM HR.tbl_Occupational_Groups g
  JOIN HR.ref_Types t ON t.Category = N'OCCUPATIONAL_GROUP_FAMILY' AND t.Name = N'Personal Académico Titular'
 WHERE g.FamilyTypeId IS NULL
   AND g.Description LIKE N'Personal Académico Titular - %';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_OccupationalGroups_Family_Degree' AND object_id = OBJECT_ID('HR.tbl_Occupational_Groups'))
    CREATE UNIQUE INDEX UX_OccupationalGroups_Family_Degree
        ON HR.tbl_Occupational_Groups (FamilyTypeId, DegreeID) WHERE FamilyTypeId IS NOT NULL;
GO

-- 3) Cargos de titulares: grupo según su nivel (las variantes de dedicación comparten el grupo de su nivel)
UPDATE j
   SET j.GroupId = g.GroupID, j.UpdatedAt = SYSDATETIME()
  FROM HR.tbl_Jobs j
  JOIN (VALUES
        (225, N'Personal Académico Titular - Auxiliar - Nivel 1'), (226, N'Personal Académico Titular - Auxiliar - Nivel 1'), (255, N'Personal Académico Titular - Auxiliar - Nivel 1'),
        (227, N'Personal Académico Titular - Auxiliar - Nivel 2'), (243, N'Personal Académico Titular - Auxiliar - Nivel 2'), (251, N'Personal Académico Titular - Auxiliar - Nivel 2'), (254, N'Personal Académico Titular - Auxiliar - Nivel 2'),
        (220, N'Personal Académico Titular - Agregado - Nivel 1'), (221, N'Personal Académico Titular - Agregado - Nivel 1'), (252, N'Personal Académico Titular - Agregado - Nivel 1'),
        (222, N'Personal Académico Titular - Agregado - Nivel 2'), (244, N'Personal Académico Titular - Agregado - Nivel 2'),
        (223, N'Personal Académico Titular - Agregado - Nivel 3'), (248, N'Personal Académico Titular - Agregado - Nivel 3'),
        (308, N'Personal Académico Titular - Principal - Nivel 1'), (249, N'Personal Académico Titular - Principal - Nivel 1'), (253, N'Personal Académico Titular - Principal - Nivel 1'),
        (250, N'Personal Académico Titular - Principal - Nivel 2')
       ) m(JobID, GroupName) ON m.JobID = j.JobID
  JOIN HR.tbl_Occupational_Groups g ON g.Description = m.GroupName
 WHERE j.GroupId IS NULL;
GO

-- 4) Grupo ocupacional del docente titular según su ESCALÓN en la estructura docente (no según su cargo).
--    El cargo de un docente a veces no refleja su nivel (promociones, cargo genérico PROFESOR, cargos de autoridad); el escalón de
--    HR.tbl_TeacherStructure sí. Se resuelve por categoría (ACADEMIC_CATEGORY) y nivel (ACADEMIC_LEVEL) por NOMBRE, no por TypeID.
--    No excluye a nadie: toda estructura vigente con categoría y nivel obtiene su grupo. Las vistas por cargo
--    (vw_PositionStructures_Unified, vw_Jobs_WithDegreeAndGroup) siguen igual.
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
JOIN HR.ref_Types rc ON rc.TypeID = l.CategoryTypeID AND rc.Category = N'ACADEMIC_CATEGORY'
JOIN HR.ref_Types rl ON rl.TypeID = l.LevelTypeID    AND rl.Category = N'ACADEMIC_LEVEL'
JOIN HR.tbl_Occupational_Groups g
  ON g.FamilyTypeId IS NOT NULL
 AND g.Description = N'Personal Académico Titular - '
       + CASE rc.Name WHEN N'TITULAR_AUXILIAR' THEN N'Auxiliar' WHEN N'TITULAR_AGREGADO' THEN N'Agregado' WHEN N'TITULAR_PRINCIPAL' THEN N'Principal' END
       + N' - Nivel ' + RIGHT(rl.Name, 1)
LEFT JOIN HR.tbl_Degrees d ON d.DegreeID = g.DegreeID
WHERE ts.IsActive = 1;
GO

-- 5) vw_Jobs_WithDegreeAndGroup: se agrega UepScaleName (clasificación adicional del grupo) para mostrarla junto al cargo
--    en el selector de cargos y en el documento de Acción de Personal. Columna nueva al final: no rompe a los lectores existentes.
CREATE OR ALTER VIEW HR.vw_Jobs_WithDegreeAndGroup AS
SELECT
    j.JobID,
    j.Description                          AS JobDescription,
    rt_type.Name                           AS JobTypeName,
    rt_regime.Name                         AS LaborRegimeName,
    og.GroupID,
    og.Description                         AS OccupationalGroup,
    og.RMU,
    d.DegreeID,
    d.Description                          AS Degree,
    d.IsActive                             AS DegreeIsActive,
    rt_uep.Name                            AS UepScaleName
FROM HR.tbl_jobs j
LEFT JOIN HR.ref_Types rt_type   ON j.JobTypeID     = rt_type.TypeID
LEFT JOIN HR.ref_Types rt_regime ON j.LaborRegimeID = rt_regime.TypeID
LEFT JOIN HR.tbl_Occupational_Groups og ON j.GroupID  = og.GroupID
LEFT JOIN HR.tbl_Degrees d              ON og.DegreeID = d.DegreeID
LEFT JOIN HR.ref_Types rt_uep           ON og.UepScaleTypeId = rt_uep.TypeID;
GO

/* ----------------------------------------------------------------------------
   Control de consistencia (solo lectura): cargos de titulares cuyo grupo no corresponde a su escalón.
   Debe devolver 0 filas. También lo avisa el formulario del cargo al elegir el escalón.

SELECT j.JobID, j.Description AS Cargo, l.Name AS Escalon, g.Description AS Grupo
  FROM HR.tbl_Jobs j
  JOIN HR.tbl_AcademicLadder l ON l.LadderID = j.AcademicLadderId
  LEFT JOIN HR.tbl_Occupational_Groups g ON g.GroupID = j.GroupId
 WHERE l.Name LIKE N'Titular %'
   AND g.Description LIKE N'Personal Académico Titular - %'
   AND g.Description <> N'Personal Académico Titular - '
         + CASE WHEN l.Name LIKE N'Titular Auxiliar%'  THEN N'Auxiliar'
                WHEN l.Name LIKE N'Titular Agregado%'  THEN N'Agregado'
                WHEN l.Name LIKE N'Titular Principal%' THEN N'Principal' END
         + N' - Nivel ' + SUBSTRING(l.Name, PATINDEX(N'%[0-9]%', l.Name), 1);
---------------------------------------------------------------------------- */
