using WsUtaSystem.Application.Common.Enums;

namespace WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;

// ── Respuestas ───────────────────────────────────────────────────────────────────

/// <summary>DTO de resumen de documento generado para listados.</summary>
public sealed record GeneratedDocumentSummaryDto(
    int DocumentId,
    int TemplateId,
    string TemplateName,
    string TemplateCode,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    DocumentEntityType EntityType,
    int? EntityId,
    string? DocumentNumber,
    string FileName,
    string Status,
    bool IsSigned,
    bool IsApproved,
    DateTime? CreatedAt,
    int? CreatedBy
);

/// <summary>DTO completo de documento generado incluyendo snapshot de campos.</summary>
public sealed record GeneratedDocumentDetailDto(
    int DocumentId,
    int TemplateId,
    string TemplateName,
    string TemplateCode,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    DocumentEntityType EntityType,
    int? EntityId,
    string? DocumentNumber,
    string FileName,
    int? StoredFileId,
    string Status,
    string? Notes,
    bool IsSigned,
    DateTime? SignedAt,
    int? SignedBy,
    bool IsApproved,
    DateTime? ApprovedAt,
    int? ApprovedBy,
    IReadOnlyList<GeneratedDocumentFieldDto> Fields,
    DateTime? CreatedAt,
    int? CreatedBy,
    DateTime? UpdatedAt,
    int? UpdatedBy
);

/// <summary>DTO del snapshot de un campo al momento de la generación.</summary>
public sealed record GeneratedDocumentFieldDto(
    int DocumentFieldId,
    string FieldName,
    string? FieldValue,
    string SourceType,
    bool WasOverridden
);

// ── Solicitudes ──────────────────────────────────────────────────────────────────

/// <summary>
/// Solicitud para generar un nuevo documento a partir de una plantilla.
/// Incluye overrides manuales opcionales para campos editables.
/// </summary>
public sealed record GenerateDocumentRequest(
    int TemplateId,
    int EmployeeId,
    DocumentEntityType EntityType,
    int? EntityId,
    string? DocumentNumber,
    string? Notes,
    Dictionary<string, string>? ManualOverrides
);

/// <summary>Respuesta de generación de documento con el PDF en base64.</summary>
public sealed record GenerateDocumentResponse(
    int DocumentId,
    string DocumentNumber,
    string FileName,
    string PdfBase64,
    int FileSizeBytes,
    IReadOnlyList<UnresolvedFieldInfo> UnresolvedFields
);

/// <summary>Información de un campo que no pudo resolverse durante la generación.</summary>
public sealed record UnresolvedFieldInfo(
    string FieldName,
    string Label,
    string? DefaultValueUsed
);

/// <summary>Solicitud para actualizar el estado de un documento generado.</summary>
public sealed record UpdateDocumentStatusRequest(
    string Status,
    string? Notes
);

/// <summary>Solicitud para aprobar un documento generado.</summary>
public sealed record ApproveDocumentRequest(
    string? Notes
);

/// <summary>Filtros para consultar documentos generados.</summary>
public sealed record DocumentQueryFilter(
    int? EmployeeId,
    int? TemplateId,
    DocumentEntityType? EntityType,
    string? Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int Page = 1,
    int PageSize = 20
);

/// <summary>Resultado paginado de documentos generados.</summary>
public sealed record PagedDocumentResult(
    IReadOnlyList<GeneratedDocumentSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
