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

// ─── Cobertura de ubicaciones por periodo ─────────────────────────────────────
// Cruza el roster activo de guardias/supervisores (una fila por cada membresía de
// grupo activa — un empleado puede estar en más de un grupo) contra las
// asignaciones del periodo, resolviendo la ubicación efectiva de cada uno con la
// misma prioridad que usa la generación de turnos: asignación individual >
// asignación de su grupo > sin asignar.

public record GuardLocationCoveragePersonDto(
    int     EmployeeId,
    string  FullName,
    int     GroupId,
    string  GroupName,
    int?    LocationId,
    string? LocationName,
    string? LocationCode,
    string  Source // "INDIVIDUAL" | "GROUP" | "UNASSIGNED"
);

public record GuardLocationCoverageLocationDto(
    int    LocationId,
    string LocationName,
    string? LocationCode,
    List<GuardLocationCoveragePersonDto> People
);

public record GuardLocationCoverageGroupDto(
    int    GroupId,
    string GroupName,
    List<GuardLocationCoveragePersonDto> People
);

public record GuardLocationCoverageResponseDto(
    int    PeriodId,
    string PeriodName,
    List<GuardLocationCoverageLocationDto> ByLocation,
    List<GuardLocationCoverageGroupDto>    ByGroup,
    List<GuardLocationCoveragePersonDto>   Unassigned
);
