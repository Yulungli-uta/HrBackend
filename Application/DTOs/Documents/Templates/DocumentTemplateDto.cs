using WsUtaSystem.Application.Common.Enums;

namespace WsUtaSystem.Application.DTOs.Documents.Templates;

// ── Respuestas ───────────────────────────────────────────────────────────────────

/// <summary>DTO de resumen de plantilla para listados.</summary>
public sealed record DocumentTemplateSummaryDto(
    int TemplateId,
    string TemplateCode,
    string Name,
    string? Description,
    string TemplateType,
    string Version,
    LayoutType LayoutType,
    DocumentTemplateStatus Status,
    bool RequiresSignature,
    bool RequiresApproval,
    int FieldCount,
    DateTime? CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>DTO completo de plantilla incluyendo campos definidos.</summary>
public sealed record DocumentTemplateDetailDto(
    int TemplateId,
    string TemplateCode,
    string Name,
    string? Description,
    string TemplateType,
    string Version,
    LayoutType LayoutType,
    DocumentTemplateStatus Status,
    string HtmlContent,
    string? CssStyles,
    string? MetaJson,
    bool RequiresSignature,
    bool RequiresApproval,
    IReadOnlyList<DocumentTemplateFieldDto> Fields,
    DateTime? CreatedAt,
    int? CreatedBy,
    DateTime? UpdatedAt,
    int? UpdatedBy
);

/// <summary>DTO de un campo de plantilla.</summary>
public sealed record DocumentTemplateFieldDto(
    int FieldId,
    int TemplateId,
    string FieldName,
    string Label,
    FieldSourceType SourceType,
    string? SourceProperty,
    string DataType,
    string? FormatPattern,
    string? DefaultValue,
    bool IsRequired,
    bool IsEditable,
    int SortOrder,
    string? HelpText
);

// ── Solicitudes ──────────────────────────────────────────────────────────────────

/// <summary>Solicitud para crear una nueva plantilla documental.</summary>
public sealed record CreateDocumentTemplateRequest(
    string TemplateCode,
    string Name,
    string? Description,
    string TemplateType,
    string Version,
    LayoutType LayoutType,
    string HtmlContent,
    string? CssStyles,
    string? MetaJson,
    bool RequiresSignature,
    bool RequiresApproval,
    IReadOnlyList<CreateDocumentTemplateFieldRequest>? Fields
);

/// <summary>Solicitud para actualizar una plantilla existente.</summary>
public sealed record UpdateDocumentTemplateRequest(
    string Name,
    string? Description,
    string Version,
    LayoutType LayoutType,
    DocumentTemplateStatus Status,
    string HtmlContent,
    string? CssStyles,
    string? MetaJson,
    bool RequiresSignature,
    bool RequiresApproval
);

/// <summary>Solicitud para crear un campo de plantilla.</summary>
public sealed record CreateDocumentTemplateFieldRequest(
    string FieldName,
    string Label,
    FieldSourceType SourceType,
    string? SourceProperty,
    string DataType,
    string? FormatPattern,
    string? DefaultValue,
    bool IsRequired,
    bool IsEditable,
    int SortOrder,
    string? HelpText
);

/// <summary>Solicitud para actualizar un campo de plantilla.</summary>
public sealed record UpdateDocumentTemplateFieldRequest(
    string Label,
    FieldSourceType SourceType,
    string? SourceProperty,
    string DataType,
    string? FormatPattern,
    string? DefaultValue,
    bool IsRequired,
    bool IsEditable,
    int SortOrder,
    string? HelpText
);

/// <summary>Solicitud para cambiar el estado de una plantilla.</summary>
public sealed record ChangeTemplateStatusRequest(
    DocumentTemplateStatus Status
);

/// <summary>Solicitud para previsualizar el HTML de una plantilla con datos de muestra.</summary>
public sealed record PreviewTemplateRequest(
    int TemplateId,
    int? EmployeeId,
    int? EntityId,
    Dictionary<string, string>? ManualOverrides
);

/// <summary>Respuesta de previsualización de plantilla.</summary>
public sealed record PreviewTemplateResponse(
    string HtmlContent,
    IReadOnlyList<UnresolvedFieldDto> UnresolvedFields
);

/// <summary>Campo que no pudo ser resuelto durante la previsualización.</summary>
public sealed record UnresolvedFieldDto(
    string FieldName,
    string Label,
    string Reason
);
