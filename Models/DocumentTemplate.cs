using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Plantilla maestra del motor documental institucional.
/// Almacena el HTML con placeholders <c>{{CAMPO}}</c> que el motor sustituye
/// con datos reales al momento de generar un documento.
/// </summary>
public sealed class DocumentTemplate : IAuditable
{
    /// <summary>Clave primaria autoincremental.</summary>
    public int TemplateId { get; set; }

    /// <summary>Código único de la plantilla (ej: <c>ACCION_PERSONAL_V1</c>).</summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo para mostrar en la interfaz.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Descripción del propósito y uso de la plantilla.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Categoría de la plantilla según <c>ref_Types.Category = 'DOCUMENT_TEMPLATE_TYPE'</c>.
    /// Ejemplos: <c>ACCION_PERSONAL</c>, <c>CONTRATO</c>, <c>OFICIO</c>.
    /// </summary>
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>Versión semántica de la plantilla (ej: <c>1.0</c>).</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Tipo de diseño que determina cómo el renderer genera el PDF.</summary>
    public LayoutType LayoutType { get; set; } = LayoutType.FlowText;

    /// <summary>Estado del ciclo de vida de la plantilla.</summary>
    public DocumentTemplateStatus Status { get; set; } = DocumentTemplateStatus.Draft;

    /// <summary>
    /// Contenido HTML de la plantilla con placeholders <c>{{CAMPO}}</c>.
    /// El motor de sustitución reemplaza cada placeholder con el valor resuelto.
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>Estilos CSS embebidos que se aplican al documento generado.</summary>
    public string? CssStyles { get; set; }

    /// <summary>Metadatos adicionales en formato JSON (márgenes, encabezado, pie, etc.).</summary>
    public string? MetaJson { get; set; }

    /// <summary>Indica si la plantilla requiere firma digital.</summary>
    public bool RequiresSignature { get; set; } = false;

    /// <summary>Indica si la plantilla requiere aprobación antes de generar el documento.</summary>
    public bool RequiresApproval { get; set; } = false;

    // ── IAuditable ──────────────────────────────────────────────────────────────
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // ── Navegación ──────────────────────────────────────────────────────────────
    /// <summary>Campos definidos para esta plantilla.</summary>
    public ICollection<DocumentTemplateField> Fields { get; set; } = [];

    /// <summary>Documentos generados a partir de esta plantilla.</summary>
    public ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = [];
}
