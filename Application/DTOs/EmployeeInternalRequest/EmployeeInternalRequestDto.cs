namespace WsUtaSystem.Application.DTOs.EmployeeInternalRequest;

public static class EmployeeInternalRequestType
{
    public const string DataUpdate = "ACTUALIZACION_DATOS";
    public const string Document = "DOCUMENTO";
    public const string Information = "INFORMACION";
    public const string Other = "OTRO";

    public static readonly string[] All = [DataUpdate, Document, Information, Other];
}

public static class EmployeeInternalRequestStatus
{
    public const string Pendiente = "PENDIENTE";
    public const string EnRevision = "EN_REVISION";
    public const string Devuelto = "DEVUELTO";
    public const string Aprobado = "APROBADO";
    public const string Rechazado = "RECHAZADO";
    public const string Anulado = "ANULADO";
    public const string Completado = "COMPLETADO";
}

public sealed record EmployeeInternalRequestSummaryDto(
    int RequestId,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    string? DepartmentName,
    string RequestType,
    string Subject,
    string Status,
    DateTime? CreatedAt
);

public sealed record EmployeeInternalRequestStatusHistoryDto(
    int HistoryId,
    int RequestId,
    string? PreviousStatus,
    string NewStatus,
    string Action,
    string? Observation,
    DateTime CreatedAt,
    int CreatedBy
);

public sealed record EmployeeInternalRequestDetailDto(
    int RequestId,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    int? DepartmentId,
    string? DepartmentName,
    string RequestType,
    string Subject,
    string? Description,
    string Status,
    DateTime? CreatedAt,
    int? CreatedBy,
    DateTime? UpdatedAt,
    DateTime? ResolvedAt,
    int? ResolvedBy,
    string? ResolvedByName,
    DateTime? CancelledAt,
    int? CancelledBy,
    IReadOnlyList<EmployeeInternalRequestStatusHistoryDto> History,
    byte[] RowVersion
);

public sealed record PagedEmployeeInternalRequestResult(
    IReadOnlyList<EmployeeInternalRequestSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>Solicitud de creación. EmployeeId nunca viene del frontend.</summary>
public sealed record CreateEmployeeInternalRequest(
    string RequestType,
    string Subject,
    string? Description
);

public sealed record UpdateEmployeeInternalRequest(
    string Subject,
    string? Description,
    byte[] RowVersion
);

public sealed record ReviewEmployeeInternalRequest(
    string? Observation,
    byte[] RowVersion
);

public sealed record CancelEmployeeInternalRequest(
    string Reason,
    byte[] RowVersion
);

public sealed record EmployeeInternalRequestQueryFilter(
    int? EmployeeId,
    string? RequestType,
    string? Status,
    int Page = 1,
    int PageSize = 20,
    IReadOnlyList<int>? AllowedDepartmentIds = null
);
