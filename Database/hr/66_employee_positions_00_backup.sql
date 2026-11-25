/* =====================================================================
   66_employee_positions_00_backup.sql
   Respaldo previo a 66_employee_positions_01_regimes_and_jobs.sql. Solo crea tablas espejo (no sobrescribe si ya existen).
   ===================================================================== */
SET NOCOUNT ON;
IF OBJECT_ID(N'HR.tbl_EmployeeLaborRegime_bak_20261126', N'U') IS NULL
    SELECT * INTO HR.tbl_EmployeeLaborRegime_bak_20261126 FROM HR.tbl_EmployeeLaborRegime;
IF OBJECT_ID(N'HR.tbl_Employees_bak_20261126', N'U') IS NULL
    SELECT * INTO HR.tbl_Employees_bak_20261126 FROM HR.tbl_Employees;
SELECT 'tbl_EmployeeLaborRegime' AS tabla, (SELECT COUNT(*) FROM HR.tbl_EmployeeLaborRegime) AS origen, (SELECT COUNT(*) FROM HR.tbl_EmployeeLaborRegime_bak_20261126) AS respaldo
UNION ALL SELECT 'tbl_Employees', (SELECT COUNT(*) FROM HR.tbl_Employees), (SELECT COUNT(*) FROM HR.tbl_Employees_bak_20261126);
