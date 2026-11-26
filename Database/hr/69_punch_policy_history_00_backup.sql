/* =====================================================================
   69_punch_policy_history_00_backup.sql
   Respaldo previo a 69_punch_policy_history_01_apply.sql. Solo crea la tabla espejo (no sobrescribe si ya existe).
   ===================================================================== */
SET NOCOUNT ON;
IF OBJECT_ID(N'HR.tbl_EmployeePunchPolicies_bak_20261126', N'U') IS NULL
    SELECT * INTO HR.tbl_EmployeePunchPolicies_bak_20261126 FROM HR.tbl_EmployeePunchPolicies;
SELECT (SELECT COUNT(*) FROM HR.tbl_EmployeePunchPolicies) AS origen,
       (SELECT COUNT(*) FROM HR.tbl_EmployeePunchPolicies_bak_20261126) AS respaldo;
