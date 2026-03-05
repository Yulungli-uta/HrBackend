/* ============================================================
   DOCFLOW - CREACIÓN DESDE CERO (MEJORADO)
   BLOQUE 3/3: VISTAS
   ============================================================ */

-- Dashboard: estado actual + proceso + depto actual + conteos
CREATE OR ALTER VIEW docflow.vw_InstanceStatus AS
SELECT
    i.InstanceId,
    i.ProcessId AS CurrentProcessId,
    p.ProcessName AS CurrentProcessName,
    i.CurrentDepartmentId,                      -- Depto responsable actual (bandeja)
    p.ResponsibleDepartmentId AS ProcessResponsibleDepartmentId,
    i.CurrentStatus,
    i.AssignedToUserId,
    u.FullName AS CreatedByName,
    i.CreatedAt,
    i.UpdatedAt,
    (SELECT COUNT(*)
     FROM docflow.tbl_Documents d
     WHERE d.InstanceId = i.InstanceId AND d.IsDeleted = 0) AS TotalDocuments,
    (SELECT COUNT(*)
     FROM docflow.tbl_Documents d
     WHERE d.InstanceId = i.InstanceId AND d.IsDeleted = 0 AND d.Visibility = 1) AS TotalPublicDocuments,
    i.DynamicMetadata
FROM docflow.tbl_WorkflowInstances i
JOIN docflow.tbl_ProcessHierarchy p ON p.ProcessId = i.ProcessId
LEFT JOIN docflow.tbl_Users u ON u.UserId = i.CreatedBy;
GO

-- Auditoría de retornos: motivo + from/to proceso/depto
CREATE OR ALTER VIEW docflow.vw_ReturnAudit AS
SELECT
    m.MovementId,
    m.InstanceId,
    m.CreatedAt AS ReturnDate,
    m.Comments AS ReturnReason,
    sender.FullName AS ReturnedByName,
    receiver.FullName AS AssignedToName,
    m.FromProcessId,
    m.ToProcessId,
    m.FromDepartmentId,
    m.ToDepartmentId,
    DATEDIFF(HOUR,
        (SELECT MAX(prev.CreatedAt)
         FROM docflow.tbl_WorkflowMovements prev
         WHERE prev.InstanceId = m.InstanceId AND prev.CreatedAt < m.CreatedAt),
        m.CreatedAt
    ) AS HoursSinceLastMovement
FROM docflow.tbl_WorkflowMovements m
JOIN docflow.tbl_Users sender ON sender.UserId = m.CreatedBy
LEFT JOIN docflow.tbl_Users receiver ON receiver.UserId = m.AssignedToUserId
WHERE m.MovementType = 'RETURN';
GO