/* ============================================================================
   33_permissions_hourtaken_widen.sql
   HR.tbl_Permissions.HourTaken pasa de DECIMAL(5,2) (máx. 999,99 minutos) a DECIMAL(9,2).
   Un permiso de 3 o más días completos (480 min/día) ya no cabía: 3 días = 1.440 min.

   El índice IX_Permissions_Employee_Status_Dates incluye HourTaken, por lo que se elimina y se
   recrea con la misma definición dentro de la misma transacción. Idempotente.
   Ejecutar con sqlcmd -f 65001.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('HR.tbl_Permissions') AND name = 'HourTaken' AND precision < 9)
BEGIN
    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('HR.tbl_Permissions') AND name = 'IX_Permissions_Employee_Status_Dates')
        DROP INDEX IX_Permissions_Employee_Status_Dates ON HR.tbl_Permissions;

    ALTER TABLE HR.tbl_Permissions ALTER COLUMN HourTaken DECIMAL(9,2) NULL;

    CREATE NONCLUSTERED INDEX IX_Permissions_Employee_Status_Dates
        ON HR.tbl_Permissions (EmployeeID, Status, StartDate, EndDate)
        INCLUDE (PermissionTypeID, ChargedToVacation, HourTaken);

    COMMIT;
END
