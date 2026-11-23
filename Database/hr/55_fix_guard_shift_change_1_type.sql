-- Guardias (QA PS-030, obs. 48): el cambio de turno 1 tiene ChangeTypeId = 1, que es "Soltero/a" del catálogo de estado civil.
-- Tiene reemplazante y no tiene nueva fecha ni ubicación: es un REEMPLAZO (GUARD_CHANGE_TYPE / REPLACEMENT).
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @replacement int = (SELECT TypeId FROM HR.ref_Types WHERE Category = N'GUARD_CHANGE_TYPE' AND Name = N'REPLACEMENT');
UPDATE HR.tbl_GuardShiftChanges SET ChangeTypeId = @replacement
WHERE ShiftChangeID = 1 AND ChangeTypeId = 1 AND ReplacementEmployeeId IS NOT NULL AND NewWorkDate IS NULL;
IF @@ROWCOUNT <> 1 OR @replacement IS NULL BEGIN ROLLBACK; THROW 50000, 'La fila no coincide con lo esperado', 1; END
COMMIT;
SELECT c.ShiftChangeID, c.ChangeTypeId, t.Name FROM HR.tbl_GuardShiftChanges c JOIN HR.ref_Types t ON t.TypeId = c.ChangeTypeId WHERE c.ShiftChangeID = 1;
