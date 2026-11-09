/* ============================================================================
   34_test_data_flag.sql
   Marca de datos de prueba en HR.tbl_Employees.

   IsTestData = 1 identifica a los empleados sintéticos de QA (5409, 5410, 5411, 5412) y al empleado 1 "Admin Admin". La
   aplicación los oculta de reportes y listados salvo que TestData:Visible = true en un entorno
   de desarrollo (ITestDataPolicy). No se borra ni modifica ningún dato de esos empleados.

   Idempotente. Ejecutar con sqlcmd -f 65001.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HR.tbl_Employees') AND name = 'IsTestData')
    ALTER TABLE HR.tbl_Employees ADD IsTestData BIT NOT NULL CONSTRAINT DF_Employees_IsTestData DEFAULT (0);
GO

BEGIN TRAN;

-- Guarda: solo las cédulas sintéticas de QA; si no son exactamente 4 empleados, aborta.
DECLARE @n INT;
SELECT @n = COUNT(*)
FROM HR.tbl_Employees e
JOIN HR.tbl_People p ON p.PersonID = e.PersonID
WHERE e.EmployeeID IN (5409, 5410, 5411, 5412)
  AND p.IDCard IN ('9999900001', '9999900002', '9999900003', '9999900004');

IF @n <> 4
BEGIN
    ROLLBACK;
    THROW 50000, 'Los empleados de prueba 5409-5412 no coinciden con las cédulas sintéticas esperadas; no se marcó ninguno.', 1;
END;

UPDATE e SET IsTestData = 1
FROM HR.tbl_Employees e
WHERE e.EmployeeID IN (5409, 5410, 5411, 5412) AND e.IsTestData = 0;

-- Empleado 1 "Admin Admin" (admin@uta.edu.ec, inactivo): contraparte HR de la cuenta local de administración de pruebas.
UPDATE e SET IsTestData = 1
FROM HR.tbl_Employees e
JOIN HR.tbl_People p ON p.PersonID = e.PersonID
WHERE e.EmployeeID = 1 AND p.IDCard = '1801234567' AND e.IsTestData = 0;

COMMIT;
GO
