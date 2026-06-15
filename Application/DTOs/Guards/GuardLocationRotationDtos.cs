namespace WsUtaSystem.Application.DTOs.Guards;

// ─── Periodos de rotación de ubicación ───────────────────────────────────────

public record GuardLocationRotationPeriodDto(
    int    LocationRotationPeriodId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool   IsActive,
    string? Notes,
    int    AssignmentCount
);

public record CreateGuardLocationRotationPeriodDto(
    string   Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string?  Notes
);

public record UpdateGuardLocationRotationPeriodDto(
    string   Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string?  Notes,
    bool     IsActive
);

// ─── Asignaciones de ubicación por periodo ────────────────────────────────────

public record GuardLocationRotationAssignmentDto(
    int     LocationRotationAssignmentId,
    int     LocationRotationPeriodId,
    string  PeriodName,
    int?    GroupId,
    string? GroupName,
    string? GroupCode,
    int?    EmployeeId,
    string? EmployeeFullName,
    string? EmployeeIdCard,
    int     LocationId,
    string  LocationName,
    string? LocationCode,
    int?    PriorityTypeId,
    string? PriorityTypeName,
    bool    IsFixedLocation,
    bool    IsFixedSchedule,
    string? Notes,
    bool    IsActive
);

public record CreateGuardLocationRotationAssignmentDto(
    int  LocationRotationPeriodId,
    int? GroupId,
    int? EmployeeId,
    int  LocationId,
    int? PriorityTypeId,
    bool IsFixedLocation,
    bool IsFixedSchedule,
    string? Notes
);

public record UpdateGuardLocationRotationAssignmentDto(
    int  LocationId,
    int? PriorityTypeId,
    bool IsFixedLocation,
    bool IsFixedSchedule,
    string? Notes,
    bool IsActive
);
