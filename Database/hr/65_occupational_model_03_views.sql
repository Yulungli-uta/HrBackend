/* =====================================================================
   65_occupational_model_03_views.sql
   vw_Jobs_WithDegreeAndGroup: agrega al FINAL el grupo ocupacional, el nivel de la escala y el estado del cargo.
   Las columnas existentes conservan nombre, orden y significado (los lectores actuales no se
   enteran). OccupationalGroup sigue devolviendo el nombre completo de la escala
   (p. ej. "Servidor Público 5") para que los documentos se impriman igual.
   Las otras vistas (vw_EmployeeAcademicGroup, vw_PositionStructures_Unified, vw_Job_Activities)
   y fn_ResolveEmployeeRate leen la misma tabla con las mismas columnas: no requieren cambio.
   ===================================================================== */
SET NOCOUNT ON;
GO
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
    rt_uep.Name                            AS UepScaleName,
    -- 2026-11-25: grupo ocupacional (familia) y nivel de la escala; columnas nuevas al final
    og.FamilyTypeId                        AS FamilyTypeId,
    rt_family.Name                         AS FamilyName,
    og.[Level]                             AS ScaleLevel,
    -- 2026-11-25: estado del cargo, para que los selectores no ofrezcan cargos inactivos
    j.IsActive                             AS JobIsActive
FROM HR.tbl_jobs j
LEFT JOIN HR.ref_Types rt_type   ON j.JobTypeID     = rt_type.TypeID
LEFT JOIN HR.ref_Types rt_regime ON j.LaborRegimeID = rt_regime.TypeID
LEFT JOIN HR.tbl_Occupational_Groups og ON j.GroupID  = og.GroupID
LEFT JOIN HR.tbl_Degrees d              ON og.DegreeID = d.DegreeID
LEFT JOIN HR.ref_Types rt_uep           ON og.UepScaleTypeId = rt_uep.TypeID
LEFT JOIN HR.ref_Types rt_family        ON og.FamilyTypeId   = rt_family.TypeID;
GO
