using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Tipo de acción de personal con control de numeración secuencial por año.
/// Reemplaza el uso de RefTypes (category='ACTION_TYPE') para este dominio.
/// </summary>
public sealed class PersonnelActionType : IAuditable
{
    /// <summary>Clave primaria.</summary>
    public int PersonnelActionTypeId { get; set; }

    /// <summary>Nombre display (ej: "Traslado", "Encargo de Funciones").</summary>
    public string Name { get; set; } = null!;

    /// <summary>Código único (ej: "TRASLADO", "ENCARGO"). Usado en mapeo de plantillas.</summary>
    public string Code { get; set; } = null!;

    /// <summary>Descripción opcional del tipo de acción.</summary>
    public string? Description { get; set; }

    /// <summary>Prefijo para numeración del documento (ej: "DAP", "REG-AP").</summary>
    public string NumberingPrefix { get; set; } = null!;

    /// <summary>Año del ciclo de numeración actual. Se reinicia la secuencia al cambiar de año.</summary>
    public int NumberingYear { get; set; } = DateTime.Now.Year;

    /// <summary>Último número de secuencia emitido para el año actual.</summary>
    public int NumberingLastSequence { get; set; } = 0;

    /// <summary>Código de la plantilla documental por defecto (ej: "ACCION_PERSONAL_V1").</summary>
    public string? TemplateCode { get; set; }

    /// <summary>Indica si este tipo de acción está disponible para uso.</summary>
    public bool IsActive { get; set; } = true;

    // ── IAuditable ──────────────────────────────────────────────────────────────
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
