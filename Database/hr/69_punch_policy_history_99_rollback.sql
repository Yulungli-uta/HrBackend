/* =====================================================================
   69_punch_policy_history_99_rollback.sql
   Reversión de 69_punch_policy_history_01_apply.sql. Exige que cada empleado tenga UNA sola fila (el índice único la requiere):
   si ya hay varias, aborta sin cambiar nada. Restaura el índice único y quita las columnas nuevas.
   ===================================================================== */
SET NOCOUNT ON; SET XACT_ABORT ON;
IF COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'RevokedAt') IS NULL THROW 50324, 'El historial no está aplicado.', 1;
IF EXISTS (SELECT 1 FROM HR.tbl_EmployeePunchPolicies GROUP BY EmployeeId HAVING COUNT(*) > 1)
    THROW 50325, 'Hay empleados con varias autorizaciones: conservar solo una por empleado antes de revertir (el historial se perdería).', 1;
BEGIN TRAN;
-- Las filas revocadas por el script vuelven a su estado original (desactivadas)
EXEC (N'UPDATE HR.tbl_EmployeePunchPolicies SET AllowExternalPunch = 0 WHERE RevokedAt IS NOT NULL;');
DROP INDEX IX_tbl_EmployeePunchPolicies_Batch ON HR.tbl_EmployeePunchPolicies;
DROP INDEX IX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies;
ALTER TABLE HR.tbl_EmployeePunchPolicies DROP CONSTRAINT FK_tbl_EmployeePunchPolicies_BatchDept;
ALTER TABLE HR.tbl_EmployeePunchPolicies DROP COLUMN RevokedAt, RevokedBy, RevokeReason, BatchId, BatchDepartmentId;
CREATE UNIQUE INDEX UX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies (EmployeeId);
COMMIT TRAN; PRINT 'REVERSIÓN APLICADA.';
