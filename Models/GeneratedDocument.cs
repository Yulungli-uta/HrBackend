using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Registro de un documento PDF generado a partir de una plantilla.
/// Actúa como cabecera del documento y referencia al archivo físico almacenado.
/// </summary>
public sealed class GeneratedDocument : IAuditable
{
    /// <summary>Clave primaria autoincremental.</summary>
    public int DocumentId { get; set; }

    /// <summary>FK a la plantilla usada para generar el documento.</summary>
    public int TemplateId { get; set; }

    /// <summary>FK al empleado al que pertenece el documento.</summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Tipo de entidad de negocio relacionada con el documento.
    /// Determina qué FK de entidad se usa (<c>EntityId</c>).
    /// </summary>
    public DocumentEntityType EntityType { get; set; }

    /// <summary>
    /// ID de la entidad de negocio relacionada (ContractId, MovementId, etc.)
    /// según el valor de <c>EntityType</c>.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>Número de documento institucional (ej: <c>DAP-2026-001</c>).</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>Nombre del archivo PDF generado.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>FK al archivo físico en <c>TBL_StoredFile</c>.</summary>
    public int? StoredFileId { get; set; }

    /// <summary>Estado del documento (DRAFT, GENERATED, SIGNED, APPROVED, REJECTED, ARCHIVED).</summary>
    public string Status { get; set; } = "DRAFT";

    /// <summary>Observaciones o notas sobre el documento.</summary>
    public string? Notes { get; set; }

    /// <summary>Indica si el documento fue firmado digitalmente.</summary>
    public bool IsSigned { get; set; } = false;

    /// <summary>Fecha y hora en que se firmó el documento.</summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>ID del usuario que firmó el documento.</summary>
    public int? SignedBy { get; set; }

    /// <summary>Indica si el documento fue aprobado.</summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>Fecha y hora en que se aprobó el documento.</summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>ID del usuario que aprobó el documento.</summary>
    public int? ApprovedBy { get; set; }

    // ── IAuditable ──────────────────────────────────────────────────────────────
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // ── Navegación ──────────────────────────────────────────────────────────────
    /// <summary>Plantilla usada para generar este documento.</summary>
    public DocumentTemplate? Template { get; set; }

    /// <summary>Valores de los campos al momento de la generación.</summary>
    public ICollection<GeneratedDocumentField> Fields { get; set; } = [];
}
