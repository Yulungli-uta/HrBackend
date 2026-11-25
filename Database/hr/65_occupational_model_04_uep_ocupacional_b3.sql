/* =====================================================================
   65_occupational_model_04_uep_ocupacional_b3.sql
   "UEP Nivel Ocupacional B3" pasa a ser un GRUPO ocupacional con su escala (como UEP Nivel Directivo B1).
   La migración 02 había movido el cargo 17 a Servidor Público 1 y dejado inactiva la escala 67; se corrige a pedido:
   - Grupo (ref_Types OCCUPATIONAL_GROUP_FAMILY) "UEP Nivel Ocupacional B3".
   - Escala 67 reactivada con ese grupo (grado 3, sin nivel, RMU referencial vacío) y cargo 17 asignado a ella.
   Uso: sqlcmd ... -v Commit=0|1 -i 65_occupational_model_04_uep_ocupacional_b3.sql   (0 = simulación)
   Idempotente: si ya se aplicó, no hace nada. Compara el sueldo que resuelve fn_ResolveEmployeeRate contra la foto previa.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit BIT = $(Commit);

IF OBJECT_ID(N'HR.snap_fn_ResolveEmployeeRate_bak_20261125', N'U') IS NULL
    THROW 50001, 'Falta el respaldo de la migración de escalas.', 1;

IF EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = N'OCCUPATIONAL_GROUP_FAMILY' AND Name = N'UEP Nivel Ocupacional B3')
BEGIN
    PRINT 'Ya aplicado; no se hace nada.';
    RETURN;
END;

BEGIN TRAN;

INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, CreatedAt, SortOrder, Metadata)
VALUES (N'OCCUPATIONAL_GROUP_FAMILY', N'UEP Nivel Ocupacional B3', N'Nivel ocupacional UEP B3', 1, SYSDATETIME(), 75, NULL);

DECLARE @fam INT = SCOPE_IDENTITY();

UPDATE HR.tbl_Occupational_Groups
SET FamilyTypeId = @fam, [Level] = NULL, Description = N'UEP Nivel Ocupacional B3', RMU = NULL,
    IsActive = 1, UpdatedAt = SYSDATETIME()
WHERE GroupID = 67;

UPDATE HR.tbl_jobs SET GroupID = 67, UpdatedAt = SYSDATETIME() WHERE JobID = 17;

-- Verificaciones
IF (SELECT COUNT(*) FROM HR.tbl_jobs) <> 243 THROW 50010, 'Cambió el número de cargos.', 1;
IF EXISTS (SELECT 1 FROM HR.tbl_jobs j JOIN HR.tbl_Occupational_Groups og ON og.GroupID = j.GroupID WHERE j.IsActive = 1 AND og.IsActive = 0)
    THROW 50011, 'Quedan cargos activos bajo una escala inactiva.', 1;

DECLARE @hoy DATE = (SELECT TOP 1 AsOfDate FROM HR.snap_fn_ResolveEmployeeRate_bak_20261125);
SELECT EmployeeID, LaborRegimeID, RMU, HourRate, SourceType, IsPrincipal INTO #now FROM HR.fn_ResolveEmployeeRate(@hoy, NULL);
SELECT EmployeeID, LaborRegimeID, RMU, HourRate, SourceType, IsPrincipal INTO #old FROM HR.snap_fn_ResolveEmployeeRate_bak_20261125;
IF EXISTS (SELECT * FROM #old EXCEPT SELECT * FROM #now) OR EXISTS (SELECT * FROM #now EXCEPT SELECT * FROM #old)
    THROW 50012, 'Cambió el sueldo resuelto de algún empleado.', 1;
DROP TABLE #now; DROP TABLE #old;

SELECT og.GroupID, og.Description, f.Name AS Grupo, d.Description AS Grado, og.RMU, uep.Name AS Uep,
       (SELECT COUNT(*) FROM HR.tbl_jobs j WHERE j.GroupID = og.GroupID) AS Cargos
FROM HR.tbl_Occupational_Groups og
JOIN HR.ref_Types f ON f.TypeID = og.FamilyTypeId
JOIN HR.tbl_Degrees d ON d.DegreeID = og.DegreeID
LEFT JOIN HR.ref_Types uep ON uep.TypeID = og.UepScaleTypeId
WHERE og.GroupID = 67;

IF @Commit = 1 BEGIN COMMIT TRAN; PRINT 'APLICADO (COMMIT).'; END
ELSE BEGIN ROLLBACK TRAN; PRINT 'SIMULACIÓN: se revirtió todo.'; END;
