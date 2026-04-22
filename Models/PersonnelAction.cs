using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Registro de una acción de personal (LOSEP / RLOSEP).
/// Captura todos los datos del formulario institucional de la UTA
/// y se vincula al documento PDF generado.
/// </summary>
public sealed class PersonnelAction : IAuditable
{
    /// <summary>Clave primaria autoincremental.</summary>
    public int ActionId { get; set; }

    /// <summary>FK al empleado afectado por la acción.</summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// FK al tipo de acción desde <c>ref_Types</c> (Category = 'ACTION_TYPE').
    /// Ejemplos: Traslado, Encargo, Licencia, Ascenso.
    /// </summary>
    public int ActionTypeId { get; set; }

    /// <summary>Número de resolución o acción (ej: <c>DAP-2026-001</c>).</summary>
    public string? ActionNumber { get; set; }

    /// <summary>Fecha en que se emite la acción de personal.</summary>
    public DateOnly ActionDate { get; set; }

    /// <summary>Fecha de inicio de la vigencia de la acción.</summary>
    public DateOnly? EffectiveDate { get; set; }

    /// <summary>Fecha de fin de la vigencia (null = indefinida).</summary>
    public DateOnly? EndDate { get; set; }

    // ── Datos del cargo origen ───────────────────────────────────────────────────
    /// <summary>ID del departamento de origen.</summary>
    public int? OriginDepartmentId { get; set; }

    /// <summary>ID del cargo de origen.</summary>
    public int? OriginJobId { get; set; }

    /// <summary>Partida presupuestaria de origen.</summary>
    public string? OriginBudgetCode { get; set; }

    // ── Datos del cargo destino ──────────────────────────────────────────────────
    /// <summary>ID del departamento de destino.</summary>
    public int? DestinationDepartmentId { get; set; }

    /// <summary>ID del cargo de destino.</summary>
    public int? DestinationJobId { get; set; }

    /// <summary>Partida presupuestaria de destino.</summary>
    public string? DestinationBudgetCode { get; set; }

    // ── Datos económicos ─────────────────────────────────────────────────────────
    /// <summary>Remuneración mensual unificada antes de la acción.</summary>
    public decimal? PreviousRmu { get; set; }

    /// <summary>Remuneración mensual unificada después de la acción.</summary>
    public decimal? NewRmu { get; set; }

    // ── Datos del documento ──────────────────────────────────────────────────────
    /// <summary>Fundamento legal de la acción (artículo LOSEP, resolución, etc.).</summary>
    public string? LegalBasis { get; set; }

    /// <summary>Motivo o justificación de la acción.</summary>
    public string? Reason { get; set; }

    /// <summary>Observaciones adicionales.</summary>
    public string? Observations { get; set; }

    /// <summary>Estado de la acción (DRAFT, APPROVED, EXECUTED, CANCELLED).</summary>
    public string Status { get; set; } = "DRAFT";

    /// <summary>FK al documento PDF generado para esta acción.</summary>
    public int? GeneratedDocumentId { get; set; }

    /// <summary>FK al contrato relacionado con la acción (si aplica).</summary>
    public int? ContractId { get; set; }

    /// <summary>FK al movimiento de personal relacionado (si aplica).</summary>
    public int? MovementId { get; set; }

    // ── IAuditable ──────────────────────────────────────────────────────────────
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // ── Navegación ──────────────────────────────────────────────────────────────
    /// <summary>Documento PDF generado para esta acción.</summary>
    public GeneratedDocument? GeneratedDocument { get; set; }
}
