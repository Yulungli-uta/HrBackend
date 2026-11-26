/* =====================================================================
   70_punch_policy_global_99_rollback.sql
   Reversión de 70_punch_policy_global_01_apply.sql. Aborta si existe alguna autorización global (no tiene empleado: se perdería).
   ===================================================================== */
SET NOCOUNT ON; SET XACT_ABORT ON;
IF COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'IsGlobal') IS NULL THROW 50335, 'La autorización global no está aplicada.', 1;
IF EXISTS (SELECT 1 FROM HR.tbl_EmployeePunchPolicies WHERE EmployeeId IS NULL) THROW 50336, 'Hay autorizaciones globales: revocarlas y retirarlas antes de revertir.', 1;
BEGIN TRAN;
DROP INDEX IX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies;
ALTER TABLE HR.tbl_EmployeePunchPolicies DROP CONSTRAINT CK_tbl_EmployeePunchPolicies_Scope;
ALTER TABLE HR.tbl_EmployeePunchPolicies DROP CONSTRAINT DF_tbl_EmployeePunchPolicies_IsGlobal;
ALTER TABLE HR.tbl_EmployeePunchPolicies DROP COLUMN IsGlobal;
ALTER TABLE HR.tbl_EmployeePunchPolicies ALTER COLUMN EmployeeId INT NOT NULL;
CREATE INDEX IX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies (EmployeeId, ValidFrom, ValidTo);
COMMIT TRAN; PRINT 'REVERSIÓN APLICADA.';
