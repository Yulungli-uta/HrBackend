/* =====================================================================
   69_punch_policy_history_01_apply.sql
   Historial de autorizaciones de picada externa: varias filas por empleado y revocación (sin borrar).
   Cambios en HR.tbl_EmployeePunchPolicies:
     - columnas RevokedAt, RevokedBy, RevokeReason, BatchId, BatchDepartmentId (nulas; el lote agrupa las autorizaciones creadas por departamento)
     - se elimina el índice único por empleado y se crea uno NO único (EmployeeId, ValidFrom, ValidTo)
     - las filas "desactivadas" (AllowExternalPunch = 0) pasan a figurar como revocadas (RevokedAt = su última modificación)
   Vigente = AllowExternalPunch = 1 AND RevokedAt IS NULL AND dentro de la vigencia. Los datos existentes no se modifican más.

   Uso (sqlcmd):  -v Commit=0  simula y revierte;  -v Commit=1  aplica (exige 69_punch_policy_history_00_backup.sql).
   Aborta con THROW si alguna verificación falla.
   ===================================================================== */
SET NOCOUNT ON; SET XACT_ABORT ON;
IF '$(Commit)' NOT IN ('0','1') THROW 50320, 'Commit debe ser 0 o 1.', 1;
IF '$(Commit)' = '1' AND OBJECT_ID(N'HR.tbl_EmployeePunchPolicies_bak_20261126', N'U') IS NULL
    THROW 50321, 'Falta el respaldo: ejecutar 69_punch_policy_history_00_backup.sql antes.', 1;
IF COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'RevokedAt') IS NOT NULL
    THROW 50322, 'El historial de autorizaciones ya fue aplicado.', 1;

DECLARE @rowsBefore INT = (SELECT COUNT(*) FROM HR.tbl_EmployeePunchPolicies);
DECLARE @effBefore INT = (SELECT COUNT(*) FROM HR.tbl_EmployeePunchPolicies
    WHERE AllowExternalPunch = 1 AND (ValidFrom IS NULL OR ValidFrom <= CAST(SYSDATETIME() AS DATE)) AND (ValidTo IS NULL OR ValidTo >= CAST(SYSDATETIME() AS DATE)));

BEGIN TRAN;

ALTER TABLE HR.tbl_EmployeePunchPolicies ADD RevokedAt DATETIME2 NULL, RevokedBy INT NULL, RevokeReason NVARCHAR(300) NULL, BatchId UNIQUEIDENTIFIER NULL, BatchDepartmentId INT NULL;
ALTER TABLE HR.tbl_EmployeePunchPolicies ADD CONSTRAINT FK_tbl_EmployeePunchPolicies_BatchDept FOREIGN KEY (BatchDepartmentId) REFERENCES HR.tbl_Departments (DepartmentID);

-- Las sentencias que usan las columnas nuevas se compilan después del ALTER
EXEC (N'UPDATE HR.tbl_EmployeePunchPolicies SET RevokedAt = ISNULL(UpdatedAt, CreatedAt), RevokedBy = UpdatedBy, RevokeReason = N''Desactivada antes del historial'' WHERE AllowExternalPunch = 0 AND RevokedAt IS NULL;');

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'HR.tbl_EmployeePunchPolicies') AND name = N'UX_tbl_EmployeePunchPolicies_Employee')
    DROP INDEX UX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies;
CREATE INDEX IX_tbl_EmployeePunchPolicies_Employee ON HR.tbl_EmployeePunchPolicies (EmployeeId, ValidFrom, ValidTo);
EXEC (N'CREATE INDEX IX_tbl_EmployeePunchPolicies_Batch ON HR.tbl_EmployeePunchPolicies (BatchId) WHERE BatchId IS NOT NULL;');

DECLARE @checks TABLE (n INT IDENTITY(1,1), prueba NVARCHAR(120), ok BIT, detalle NVARCHAR(200));
DECLARE @rowsAfter INT, @effAfter INT, @dupSample INT;
SELECT @rowsAfter = COUNT(*) FROM HR.tbl_EmployeePunchPolicies;
EXEC sp_executesql
    N'SELECT @e = COUNT(*) FROM HR.tbl_EmployeePunchPolicies
      WHERE AllowExternalPunch = 1 AND RevokedAt IS NULL AND (ValidFrom IS NULL OR ValidFrom <= CAST(SYSDATETIME() AS DATE)) AND (ValidTo IS NULL OR ValidTo >= CAST(SYSDATETIME() AS DATE))',
    N'@e INT OUTPUT', @e = @effAfter OUTPUT;

INSERT @checks (prueba, ok, detalle) VALUES
 (N'No se perdió ninguna fila', IIF(@rowsAfter = @rowsBefore, 1, 0), CONCAT(@rowsBefore, N' -> ', @rowsAfter)),
 (N'Las autorizaciones vigentes son las mismas', IIF(@effAfter = @effBefore, 1, 0), CONCAT(@effBefore, N' -> ', @effAfter)),
 (N'Existen las 5 columnas nuevas', IIF(COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'BatchId') IS NOT NULL AND COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'BatchDepartmentId') IS NOT NULL AND COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'RevokedAt') IS NOT NULL AND COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'RevokedBy') IS NOT NULL AND COL_LENGTH(N'HR.tbl_EmployeePunchPolicies', N'RevokeReason') IS NOT NULL, 1, 0), N''),
 (N'El índice único ya no existe', IIF(NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'HR.tbl_EmployeePunchPolicies') AND name = N'UX_tbl_EmployeePunchPolicies_Employee'), 1, 0), N''),
 (N'Existe el índice nuevo', IIF(EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'HR.tbl_EmployeePunchPolicies') AND name = N'IX_tbl_EmployeePunchPolicies_Employee'), 1, 0), N'');

SELECT n, prueba, ok, detalle FROM @checks;
IF EXISTS (SELECT 1 FROM @checks WHERE ok = 0) BEGIN ROLLBACK TRAN; THROW 50323, 'Una verificación falló: todo revertido.', 1; END

IF '$(Commit)' = '1' BEGIN COMMIT TRAN; PRINT 'APLICADO.'; END
ELSE BEGIN ROLLBACK TRAN; PRINT 'SIMULACIÓN: todo revertido (Commit=0).'; END
