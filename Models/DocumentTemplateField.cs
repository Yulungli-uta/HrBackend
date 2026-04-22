using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Define un campo (placeholder) de una plantilla documental.
/// Cada campo especifica de dónde provienen sus datos y cómo se formatea.
/// </summary>
public sealed class DocumentTemplateField : IAuditable
{
    /// <summary>Clave primaria autoincremental.</summary>
    public int FieldId { get; set; }

    /// <summary>FK a la plantilla propietaria.</summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Nombre del placeholder tal como aparece en el HTML (sin llaves).
    /// Ejemplo: <c>NOMBRE_EMPLEADO</c> para el placeholder <c>{{NOMBRE_EMPLEADO}}</c>.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Etiqueta legible para mostrar en la interfaz de usuario.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Origen de los datos que resuelve este campo.</summary>
    public FieldSourceType SourceType { get; set; } = FieldSourceType.System;

    /// <summary>
    /// Nombre de la propiedad o columna de la fuente de datos.
    /// Ejemplo: <c>People.FirstName + ' ' + People.LastName</c> o <c>Contracts.StartDate</c>.
    /// </summary>
    public string? SourceProperty { get; set; }

    /// <summary>Tipo de dato del campo (TEXT, DATE, NUMBER, BOOLEAN).</summary>
    public string DataType { get; set; } = "TEXT";

    /// <summary>Formato de presentación (ej: <c>dd/MM/yyyy</c>, <c>N2</c>, <c>UPPER</c>).</summary>
    public string? FormatPattern { get; set; }

    /// <summary>Valor por defecto cuando el campo no puede resolverse.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Indica si el campo es obligatorio para generar el documento.</summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>Indica si el campo puede ser sobreescrito manualmente por el usuario.</summary>
    public bool IsEditable { get; set; } = false;

    /// <summary>Orden de presentación en el formulario de generación.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Descripción de ayuda para el usuario.</summary>
    public string? HelpText { get; set; }

    // ── IAuditable ──────────────────────────────────────────────────────────────
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // ── Navegación ──────────────────────────────────────────────────────────────
    /// <summary>Plantilla a la que pertenece este campo.</summary>
    public DocumentTemplate? Template { get; set; }
}
