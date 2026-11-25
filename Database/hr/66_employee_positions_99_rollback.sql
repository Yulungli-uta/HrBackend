/* =====================================================================
   66_employee_positions_99_rollback.sql
   Reversión de 66_employee_positions_01: elimina las filas de régimen nuevas y restaura cargo/unidad de las filas y fichas
   desde los respaldos *_bak_20261126. Una sola transacción.
   ===================================================================== */
SET NOCOUNT ON; SET XACT_ABORT ON;
IF OBJECT_ID(N'HR.tbl_EmployeeLaborRegime_bak_20261126', N'U') IS NULL THROW 50090, 'No existe el respaldo.', 1;
BEGIN TRAN;
DELETE FROM HR.tbl_EmployeeLaborRegime WHERE Id NOT IN (SELECT Id FROM HR.tbl_EmployeeLaborRegime_bak_20261126);
UPDATE r SET r.JobId = b.JobId, r.DepartmentId = b.DepartmentId, r.UpdatedAt = b.UpdatedAt, r.UpdatedBy = b.UpdatedBy
FROM HR.tbl_EmployeeLaborRegime r JOIN HR.tbl_EmployeeLaborRegime_bak_20261126 b ON b.Id = r.Id
WHERE ISNULL(r.JobId,-1) <> ISNULL(b.JobId,-1) OR ISNULL(r.DepartmentId,-1) <> ISNULL(b.DepartmentId,-1);
UPDATE e SET e.JobID = b.JobID, e.DepartmentID = b.DepartmentID, e.UpdatedAt = b.UpdatedAt, e.UpdatedBy = b.UpdatedBy
FROM HR.tbl_Employees e JOIN HR.tbl_Employees_bak_20261126 b ON b.EmployeeID = e.EmployeeID
WHERE ISNULL(e.JobID,-1) <> ISNULL(b.JobID,-1) OR ISNULL(e.DepartmentID,-1) <> ISNULL(b.DepartmentID,-1);
IF (SELECT COUNT(*) FROM HR.tbl_EmployeeLaborRegime) <> (SELECT COUNT(*) FROM HR.tbl_EmployeeLaborRegime_bak_20261126) THROW 50091, 'La reversión no dejó el mismo número de regímenes.', 1;
COMMIT TRAN; PRINT 'REVERSIÓN APLICADA.';
