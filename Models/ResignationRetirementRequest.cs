using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Solicitud de renuncia o jubilación registrada por el empleado autenticado
/// y revisada por Recursos Humanos.
/// </summary>
public sealed class ResignationRetirementRequest : IAuditable
{
    public int RequestId { get; set; }

    /// <summary>FK al empleado solicitante. Siempre resuelto en backend desde el usuario autenticado.</summary>
    public int EmployeeId { get; set; }

    /// <summary>RESIGNATION o RETIREMENT.</summary>
    public string RequestType { get; set; } = string.Empty;

    public DateOnly RequestDate { get; set; }

    public DateOnly ProposedExitDate { get; set; }

    public string? Reason { get; set; }

    public string? AdditionalNotes { get; set; }

    /// <summary>PENDIENTE, EN_REVISION, DEVUELTO, APROBADO, RECHAZADO, ANULADO.</summary>
    public string Status { get; set; } = "PENDIENTE";

    /// <summary>FK reservado a la acción de personal de desvinculación, si RRHH la genera luego de aprobar.</summary>
    public int? LinkedPersonnelActionId { get; set; }

    /// <summary>FK al documento PDF generado (carta de renuncia/jubilación) para firma.</summary>
    public int? GeneratedDocumentId { get; set; }

    // ── IAuditable ──────────────────────────────────────────────────────────────
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledBy { get; set; }

    public byte[]? RowVersion { get; set; }

    // ── Navegación ──────────────────────────────────────────────────────────────
    public Employees? Employee { get; set; }
    public ICollection<ResignationRetirementStatusHistory> StatusHistory { get; set; } = [];
}
