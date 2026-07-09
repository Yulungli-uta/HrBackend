namespace WsUtaSystem.Application.DTOs.EmployeeCertificate;

public static class EmployeeCertificateType
{
    public const string Laboral = "LABORAL";
    public const string HistorialLaboral = "HISTORIAL_LABORAL";
}

/// <summary>Una fila del historial laboral (contrato o acción de personal) para el certificado.</summary>
public sealed record EmploymentHistoryEntry(
    string SourceType, // CONTRACT | PERSONNEL_ACTION
    string? DocumentNumber,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? JobTitle,
    string? DepartmentName,
    string? StatusLabel
);

public static class EmployeeCertificateStatus
{
    public const string Pendiente = "PENDIENTE";
    public const string Emitido = "EMITIDO";
    public const string Rechazado = "RECHAZADO";
    public const string Anulado = "ANULADO";
}

public sealed record EmployeeCertificateSummaryDto(
    int RequestId,
    int EmployeeId,
    string CertificateType,
    string? Purpose,
    string Status,
    int? GeneratedDocumentId,
    DateTime? CreatedAt,
    DateTime? IssuedAt
);

public sealed record EmployeeCertificateStatusHistoryDto(
    int HistoryId,
    int RequestId,
    string? PreviousStatus,
    string NewStatus,
    string Action,
    string? Observation,
    DateTime CreatedAt,
    int CreatedBy
);

public sealed record EmployeeCertificateDetailDto(
    int RequestId,
    int EmployeeId,
    string EmployeeFullName,
    int? DepartmentId,
    string CertificateType,
    string? Purpose,
    string Status,
    int? GeneratedDocumentId,
    string? GeneratedDocumentFileName,
    DateTime? CreatedAt,
    DateTime? IssuedAt,
    IReadOnlyList<EmployeeCertificateStatusHistoryDto> History,
    byte[] RowVersion
);

/// <summary>Solicitud de creación. EmployeeId siempre resuelto desde el usuario autenticado.</summary>
public sealed record CreateEmployeeCertificateRequest(
    string CertificateType,
    string? Purpose
);

public sealed record PagedEmployeeCertificateResult(
    IReadOnlyList<EmployeeCertificateSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record EmployeeCertificateQueryFilter(
    int? EmployeeId,
    string? Status,
    int Page = 1,
    int PageSize = 20,
    IReadOnlyList<int>? AllowedDepartmentIds = null
);
