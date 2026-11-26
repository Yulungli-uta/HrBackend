/* =====================================================================
   70_punch_policy_global_01_apply.sql
   Autorización GLOBAL de picada externa (toda la institución) como UNA sola fila, sin empleado.
   Cambios en HR.tbl_EmployeePunchPolicies: columna IsGlobal (0 por defecto), EmployeeId pasa a admitir NULL y una restricción exige
   coherencia (global = sin empleado; individual = con empleado). Las filas existentes quedan como individuales, sin cambios.

   Uso (sqlcmd):  -v Commit=0 simula y revierte;  -v Commit=1 aplica (exige 70_punch_policy_global_00_backup.sql y haber aplicado el 69).
   ===================================================================== */
SET NOCOUNT ON; SET XACT_ABORT ON;
IF '$(Commit)' NOT IN ('0','1') THROW 50330, 'Commit debe ser 0 o 1.', 1;
IF COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'RevokedAt') IS NULL THROW 50331, 'Falta aplicar 69_punch_policy_history_01_apply.sql.', 1;
IF '$(Commit)' = '1' AND OBJECT_ID(N'HR.tbl_EmployeePunchPolicies_bak2_20261126', N'U') IS NULL THROW 50332, 'Falta el respaldo: ejecutar 70_punch_policy_global_00_backup.sql antes.', 1;
IF COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'IsGlobal') IS NOT NULL THROW 50333, 'La autorización global ya fue aplicada.', 1;

DECLARE @rowsBefore INT = (SELECT COUNT(*) FROM HR.tbl_EmployeePunchPolicies);
BEGIN TRAN;

-- EmployeeId está en el índice (EmployeeId, ValidFrom, ValidTo): se recrea al cambiar su nulabilidad
DROP INDEX IX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies;
ALTER TABLE HR.tbl_EmployeePunchPolicies ALTER COLUMN EmployeeId INT NULL;
ALTER TABLE HR.tbl_EmployeePunchPolicies ADD IsGlobal BIT NOT NULL CONSTRAINT DF_tbl_EmployeePunchPolicies_IsGlobal DEFAULT (0);
EXEC (N'ALTER TABLE HR.tbl_EmployeePunchPolicies ADD CONSTRAINT CK_tbl_EmployeePunchPolicies_Scope CHECK ((IsGlobal = 1 AND EmployeeId IS NULL) OR (IsGlobal = 0 AND EmployeeId IS NOT NULL));');
CREATE INDEX IX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies (EmployeeId, ValidFrom, ValidTo);

DECLARE @checks TABLE (n INT IDENTITY(1,1), prueba NVARCHAR(120), ok BIT, detalle NVARCHAR(200));
DECLARE @rowsAfter INT, @indiv INT;
SELECT @rowsAfter = COUNT(*) FROM HR.tbl_EmployeePunchPolicies;
EXEC sp_executesql N'SELECT @i = COUNT(*) FROM HR.tbl_EmployeePunchPolicies WHERE IsGlobal = 0 AND EmployeeId IS NOT NULL', N'@i INT OUTPUT', @i = @indiv OUTPUT;
INSERT @checks (prueba, ok, detalle) VALUES
 (N'No se perdió ninguna fila', IIF(@rowsAfter = @rowsBefore, 1, 0), CONCAT(@rowsBefore, N' -> ', @rowsAfter)),
 (N'Todas las filas quedan como individuales', IIF(@indiv = @rowsAfter, 1, 0), CONCAT(@indiv, N' de ', @rowsAfter)),
 (N'EmployeeId admite NULL', IIF(COLUMNPROPERTY(OBJECT_ID(N'HR.tbl_EmployeePunchPolicies'), N'EmployeeId', 'AllowsNull') = 1, 1, 0), N''),
 (N'Existe el índice', IIF(EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'HR.tbl_EmployeePunchPolicies') AND name = N'IX_tbl_EmployeePunchPolicies_Employee'), 1, 0), N'');
SELECT n, prueba, ok, detalle FROM @checks;
IF EXISTS (SELECT 1 FROM @checks WHERE ok = 0) BEGIN ROLLBACK TRAN; THROW 50334, 'Una verificación falló: todo revertido.', 1; END

IF '$(Commit)' = '1' BEGIN COMMIT TRAN; PRINT 'APLICADO.'; END
ELSE BEGIN ROLLBACK TRAN; PRINT 'SIMULACIÓN: todo revertido (Commit=0).'; END
