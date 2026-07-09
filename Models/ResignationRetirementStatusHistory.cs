namespace WsUtaSystem.Models;

/// <summary>
/// Registro de auditoría de cada cambio de estado en una solicitud de renuncia/jubilación.
/// </summary>
public sealed class ResignationRetirementStatusHistory
{
    public int HistoryId { get; set; }

    /// <summary>FK a la solicitud.</summary>
    public int RequestId { get; set; }

    public string? PreviousStatus { get; set; }

    public string NewStatus { get; set; } = string.Empty;

    /// <summary>CREATED, SUBMITTED, UPDATED, APPROVED, REJECTED, RETURNED, CANCELLED.</summary>
    public string Action { get; set; } = string.Empty;

    public string? Observation { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    // ── Navegación ──────────────────────────────────────────────────────────────
    public ResignationRetirementRequest? Request { get; set; }
}
