/* =====================================================================
   65_occupational_model_99_rollback.sql
   Reversión de la reestructuración Cargo -> Escala. Restaura desde los respaldos *_bak_20261125.
   Uso: sqlcmd ... -i 65_occupational_model_99_rollback.sql   (una sola transacción)
   No toca empleados, contratos, acciones ni sueldos: solo cargos, escalas, grados y grupos.
   La columna Level y el RMU nulable (estructura) se dejan: son compatibles.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'HR.tbl_jobs_bak_20261125', N'U') IS NULL
    THROW 50090, 'No existe el respaldo; no se puede revertir.', 1;

BEGIN TRAN;

-- Cargos: grupo, descripción y estado como estaban
UPDATE j SET j.GroupID = b.GroupID, j.IsActive = b.IsActive, j.UpdatedAt = b.UpdatedAt, j.UpdatedBy = b.UpdatedBy
FROM HR.tbl_jobs j JOIN HR.tbl_jobs_bak_20261125 b ON b.JobID = j.JobID
WHERE ISNULL(j.GroupID, -1) <> ISNULL(b.GroupID, -1) OR j.IsActive <> b.IsActive;

-- Escalas: se quitan las nuevas (si ningún cargo las usa) y se restauran las demás
UPDATE HR.tbl_jobs SET GroupID = NULL
WHERE GroupID IN (SELECT GroupID FROM HR.tbl_Occupational_Groups
                  WHERE GroupID NOT IN (SELECT GroupID FROM HR.tbl_Occupational_Groups_bak_20261125))
  AND JobID IN (SELECT JobID FROM HR.tbl_jobs_bak_20261125 WHERE GroupID IS NULL);
DELETE FROM HR.tbl_Occupational_Groups
WHERE GroupID NOT IN (SELECT GroupID FROM HR.tbl_Occupational_Groups_bak_20261125)
  AND GroupID NOT IN (SELECT GroupID FROM HR.tbl_jobs WHERE GroupID IS NOT NULL);

UPDATE og SET og.Description = b.Description, og.RMU = b.RMU, og.DegreeID = b.DegreeID, og.IsActive = b.IsActive,
              og.UepScaleTypeId = b.UepScaleTypeId, og.FamilyTypeId = b.FamilyTypeId, og.[Level] = NULL,
              og.UpdatedAt = b.UpdatedAt, og.UpdatedBy = b.UpdatedBy
FROM HR.tbl_Occupational_Groups og JOIN HR.tbl_Occupational_Groups_bak_20261125 b ON b.GroupID = og.GroupID;

-- Grados
UPDATE d SET d.Description = b.Description, d.IsActive = b.IsActive, d.UpdatedAt = b.UpdatedAt
FROM HR.tbl_Degrees d JOIN HR.tbl_Degrees_bak_20261125 b ON b.DegreeID = d.DegreeID;

-- Grupos (familias) de ref_Types: se quitan las creadas por la migración y se restaura la anterior
DELETE FROM HR.ref_Types
WHERE Category = N'OCCUPATIONAL_GROUP_FAMILY'
  AND TypeID NOT IN (SELECT TypeID FROM HR.ref_Types_bak_20261125)
  AND TypeID NOT IN (SELECT FamilyTypeId FROM HR.tbl_Occupational_Groups WHERE FamilyTypeId IS NOT NULL);
UPDATE r SET r.IsActive = b.IsActive, r.Name = b.Name, r.Description = b.Description, r.Metadata = b.Metadata
FROM HR.ref_Types r JOIN HR.ref_Types_bak_20261125 b ON b.TypeID = r.TypeID;

-- Verificación: los cargos vuelven a su grupo original
IF EXISTS (SELECT JobID, GroupID FROM HR.tbl_jobs EXCEPT SELECT JobID, GroupID FROM HR.tbl_jobs_bak_20261125)
    THROW 50091, 'La reversión no dejó los cargos como en el respaldo; se cancela.', 1;

COMMIT TRAN;
PRINT 'REVERSIÓN APLICADA. Restaurar vistas anteriores desde Database/backups_20261125/modules_before_migration.sql si se necesita.';
