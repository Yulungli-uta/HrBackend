-- ============================================================
-- Feature: corrección auditable de Acciones de Personal y
-- Contratos (análisis aprobado por el usuario 2026-08-05).
--
-- No crea tablas nuevas: reutiliza HR.tbl_Audit, que hasta ahora
-- solo se poblaba en DELETE (AuditSaveChangesInterceptor). Las
-- correcciones manuales (PersonnelActionService.CorrectAsync /
-- ContractsService.CorrectAsync) insertan ahí una fila con
-- Action='CORRECTION' y el diff de campos (viejo/nuevo) + motivo
-- serializado en Details.
--
-- Único cambio de esquema real: 2 índices sobre HR.tbl_Audit que
-- no existían (la tabla no tenía ninguno), necesarios para que la
-- pantalla "Historial de Correcciones" (filtro por tabla/entidad,
-- usuario y rango de fechas) no haga table scan.
--
-- Solo aditivo / idempotente — seguro de re-ejecutar.
-- ============================================================

SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Audit_TableName_RecordID'
      AND object_id = OBJECT_ID('[HR].[tbl_Audit]')
)
CREATE INDEX [IX_Audit_TableName_RecordID]
    ON [HR].[tbl_Audit] ([TableName], [RecordID]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Audit_ActionDate'
      AND object_id = OBJECT_ID('[HR].[tbl_Audit]')
)
CREATE INDEX [IX_Audit_ActionDate]
    ON [HR].[tbl_Audit] ([ActionDate] DESC);
GO
