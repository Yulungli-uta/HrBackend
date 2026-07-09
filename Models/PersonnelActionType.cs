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

    /// <summary>FK a la plantilla documental (DocumentTemplate.TemplateId) usada por defecto para este tipo de acción.</summary>
    public int? DefaultTemplateId { get; set; }

    /// <summary>Plantilla documental usada por defecto.</summary>
    public DocumentTemplate? DefaultTemplate { get; set; }

    /// <summary>Indica si este tipo de acción está disponible para uso.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Categoría funcional de la acción. Permite agrupar y filtrar en reportes.
    /// Valores en uso: MOVEMENT, ENTRY, SALARY_CHANGE, SCHEDULE, LEAVE, DISCIPLINARY, VULNERABILITY, VACATION.
    /// </summary>
    public string? ActionCategory { get; set; }

    /// <summary>
    /// 2026-07-06: si es true, esta acción participa en la cadena de "acción vigente"
    /// del empleado — al cargar el documento firmado, transiciona automáticamente
    /// FIRMADO_CARGADO → VIGENTE (en vez del flujo manual a FINALIZADO), y cierra a
    /// FINALIZADO la acción que estuviera vigente para ese empleado en ese momento.
    /// </summary>
    public bool ReachesVigente { get; set; } = false;

    // ── Integración Active Directory ─────────────────────────────────────────────

    /// <summary>Si verdadero, al ejecutar esta acción se debe crear un usuario en AD local.</summary>
    public bool RequiresAdUserCreation { get; set; } = false;

    /// <summary>Si verdadero, al ejecutar esta acción se debe deshabilitar el usuario en AD local (ej: baja, jubilación).</summary>
    public bool RequiresAdUserDisable { get; set; } = false;

    /// <summary>Si verdadero, al ejecutar esta acción se deben asignar grupos/roles en AD local al usuario.</summary>
    public bool RequiresAdGroupAssignment { get; set; } = false;

    // ── IAuditable ──────────────────────────────────────────────────────────────
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
