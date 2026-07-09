using WsUtaSystem.Application.DTOs.EmployeeCertificate;
using WsUtaSystem.Application.DTOs.EmployeeInternalRequest;

namespace WsUtaSystem.Application.DTOs.EmployeeSelfService;

public sealed record EmployeeSelfServiceProfileDto(
    int EmployeeId,
    string FullName,
    string IdCard,
    string? Email,
    string? PersonalEmail,
    string? JobTitle,
    int? DepartmentId,
    string? DepartmentName,
    string? ContractType,
    string? Schedule,
    DateTime HireDate,
    int? ImmediateBossId
);

public sealed record EmployeeSelfServicePermissionDto(
    int PermissionId,
    int PermissionTypeId,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    decimal? HourTaken,
    string? Justification
);

public sealed record EmployeeSelfServiceVacationDto(
    int VacationId,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysGranted,
    int DaysTaken,
    string Status
);

public sealed record EmployeeSelfServiceSummaryDto(
    EmployeeSelfServiceProfileDto Profile,
    decimal VacationAvailableDays,
    int PendingPermissionsCount,
    int PendingInternalRequestsCount,
    IReadOnlyList<EmployeeSelfServicePermissionDto> RecentPermissions,
    IReadOnlyList<EmployeeSelfServiceVacationDto> RecentVacations,
    IReadOnlyList<EmployeeCertificateSummaryDto> RecentCertificates,
    IReadOnlyList<EmployeeInternalRequestSummaryDto> RecentInternalRequests,
    DateTime? LastPunchTime,
    string? LastPunchType,
    int PendingJustificationsCount
);

public sealed record EmployeeSelfServiceHistoryEntryDto(
    string Source, // PERMISSION | VACATION | CERTIFICATE | INTERNAL_REQUEST
    int SourceId,
    string Title,
    string Status,
    DateTime Date,
    string? Description
);
