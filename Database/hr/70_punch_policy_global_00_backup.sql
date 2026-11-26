/* =====================================================================
   70_punch_policy_global_00_backup.sql
   Respaldo previo a 70_punch_policy_global_01_apply.sql (tabla espejo; no sobrescribe si ya existe).
   ===================================================================== */
SET NOCOUNT ON;
IF OBJECT_ID(N'HR.tbl_EmployeePunchPolicies_bak2_20261126', N'U') IS NULL
    SELECT * INTO HR.tbl_EmployeePunchPolicies_bak2_20261126 FROM HR.tbl_EmployeePunchPolicies;
SELECT (SELECT COUNT(*) FROM HR.tbl_EmployeePunchPolicies) AS origen, (SELECT COUNT(*) FROM HR.tbl_EmployeePunchPolicies_bak2_20261126) AS respaldo;
