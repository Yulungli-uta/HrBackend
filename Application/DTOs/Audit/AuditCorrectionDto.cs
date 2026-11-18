namespace WsUtaSystem.Application.DTOs.Audit;

/// <summary>
/// Corrección manual de HR.Audit enriquecida con los datos que identifican el registro corregido
/// (número de acción o de contrato, persona y tipo), para que el historial no muestre solo un ID interno.
/// </summary>
public sealed class AuditCorrectionDto : AuditDto
{
    /// <summary>Número visible del registro: <c>ActionNumber</c> de la acción de personal o <c>ContractCode</c> del contrato. Null si no existe o no es visible.</summary>
    public string? RecordNumber { get; set; }

    /// <summary>Apellidos y nombres de la persona afectada por el registro corregido.</summary>
    public string? PersonName { get; set; }

    /// <summary>Tipo de acción de personal (ej: "Traslado"). Null para contratos.</summary>
    public string? RecordTypeName { get; set; }

    /// <summary>Estado actual del registro (acciones de personal).</summary>
    public string? RecordStatus { get; set; }
}

/// <summary>Datos de contexto de un registro auditado, resueltos por <c>IAuditRepository</c>.</summary>
/// <param name="TableName">Tabla auditada (<c>PersonnelActions</c> o <c>Contracts</c>).</param>
/// <param name="RecordId">ID interno del registro, como texto (igual que HR.Audit.RecordID).</param>
/// <param name="RecordNumber">Número de acción o código de contrato.</param>
/// <param name="PersonName">Apellidos y nombres de la persona.</param>
/// <param name="RecordTypeName">Nombre del tipo de acción de personal.</param>
/// <param name="RecordStatus">Estado actual de la acción de personal.</param>
public sealed record AuditRecordContext(
    string TableName,
    string RecordId,
    string? RecordNumber,
    string? PersonName,
    string? RecordTypeName,
    string? RecordStatus);
