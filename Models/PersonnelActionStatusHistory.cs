namespace WsUtaSystem.Models;

/// <summary>
/// Registro de auditoría de cada cambio de estado en una Acción de Personal.
/// </summary>
public sealed class PersonnelActionStatusHistory
{
    public int HistoryId { get; set; }

    /// <summary>FK a la acción de personal.</summary>
    public int ActionId { get; set; }

    /// <summary>FK a <c>HR.ref_Types</c> (Category = 'PERSONNEL_ACTION_STATUS'). Nullable para degradación graceful si el catálogo no está sembrado.</summary>
    public int? StatusTypeId { get; set; }

    /// <summary>Código del estado anterior al cambio.</summary>
    public string? FromStatus { get; set; }

    /// <summary>Código del estado destino del cambio (snapshot denormalizado para lectura rápida).</summary>
    public string StatusCode { get; set; } = string.Empty;

    /// <summary>Observación o motivo del cambio.</summary>
    public string? Comment { get; set; }

    /// <summary>ID del usuario que realizó el cambio.</summary>
    public int? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    // ── Navegación ──────────────────────────────────────────────────────────────
    public PersonnelAction? Action { get; set; }
    public RefTypes? StatusType { get; set; }
}
