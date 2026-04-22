namespace WsUtaSystem.Application.DTOs.Documents.PersonnelActions;

// ── Respuestas ───────────────────────────────────────────────────────────────────

/// <summary>DTO de resumen de acción de personal para listados.</summary>
public sealed record PersonnelActionSummaryDto(
    int ActionId,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    int ActionTypeId,
    string ActionTypeName,
    string? ActionNumber,
    DateOnly ActionDate,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,
    string Status,
    int? GeneratedDocumentId,
    DateTime? CreatedAt
);

/// <summary>DTO completo de acción de personal con todos los datos del formulario.</summary>
public sealed record PersonnelActionDetailDto(
    int ActionId,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    string EmployeeDepartment,
    string EmployeeJobTitle,
    int ActionTypeId,
    string ActionTypeName,
    string? ActionNumber,
    DateOnly ActionDate,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,

    // Cargo origen
    int? OriginDepartmentId,
    string? OriginDepartmentName,
    int? OriginJobId,
    string? OriginJobTitle,
    string? OriginBudgetCode,

    // Cargo destino
    int? DestinationDepartmentId,
    string? DestinationDepartmentName,
    int? DestinationJobId,
    string? DestinationJobTitle,
    string? DestinationBudgetCode,

    // Datos económicos
    decimal? PreviousRmu,
    decimal? NewRmu,

    // Datos del documento
    string? LegalBasis,
    string? Reason,
    string? Observations,
    string Status,

    // Relaciones
    int? GeneratedDocumentId,
    string? GeneratedDocumentFileName,
    int? ContractId,
    int? MovementId,

    // Auditoría
    DateTime? CreatedAt,
    int? CreatedBy,
    DateTime? UpdatedAt,
    int? UpdatedBy
);

// ── Solicitudes ──────────────────────────────────────────────────────────────────

/// <summary>Solicitud para crear una nueva acción de personal.</summary>
public sealed record CreatePersonnelActionRequest(
    int EmployeeId,
    int ActionTypeId,
    string? ActionNumber,
    DateOnly ActionDate,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,

    // Cargo origen
    int? OriginDepartmentId,
    int? OriginJobId,
    string? OriginBudgetCode,

    // Cargo destino
    int? DestinationDepartmentId,
    int? DestinationJobId,
    string? DestinationBudgetCode,

    // Datos económicos
    decimal? PreviousRmu,
    decimal? NewRmu,

    // Datos del documento
    string? LegalBasis,
    string? Reason,
    string? Observations,

    // Relaciones
    int? ContractId,
    int? MovementId,

    // Generación automática del documento PDF
    bool GenerateDocument = false,
    Dictionary<string, string>? DocumentOverrides = null
);

/// <summary>Solicitud para actualizar una acción de personal existente.</summary>
public sealed record UpdatePersonnelActionRequest(
    string? ActionNumber,
    DateOnly ActionDate,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,
    int? OriginDepartmentId,
    int? OriginJobId,
    string? OriginBudgetCode,
    int? DestinationDepartmentId,
    int? DestinationJobId,
    string? DestinationBudgetCode,
    decimal? PreviousRmu,
    decimal? NewRmu,
    string? LegalBasis,
    string? Reason,
    string? Observations
);

/// <summary>Solicitud para aprobar y ejecutar una acción de personal.</summary>
public sealed record ApprovePersonnelActionRequest(
    string? Notes,
    bool GenerateDocumentIfMissing = true
);

/// <summary>Filtros para consultar acciones de personal.</summary>
public sealed record PersonnelActionQueryFilter(
    int? EmployeeId,
    int? ActionTypeId,
    string? Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int Page = 1,
    int PageSize = 20
);

/// <summary>Resultado paginado de acciones de personal.</summary>
public sealed record PagedPersonnelActionResult(
    IReadOnlyList<PersonnelActionSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>Respuesta de creación de acción de personal con documento generado opcional.</summary>
public sealed record CreatePersonnelActionResponse(
    int ActionId,
    string? ActionNumber,
    string Status,
    int? GeneratedDocumentId,
    string? PdfBase64,
    string? FileName
);
