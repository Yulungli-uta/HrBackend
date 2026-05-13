namespace WsUtaSystem.Application.DTOs.Guards;

public record EmployeeAvailabilityBlockDto(
    int BlockId,
    int EmployeeId,
    string EmployeeFullName,
    string SourceType,
    string? SourceTable,
    string? SourceId,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Status,
    string? Reason
);

public record CreateManualAvailabilityBlockDto(
    int EmployeeId,
    int SourceTypeId,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? Reason
);

public record EmployeeAvailabilityFilterDto(
    int? EmployeeId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? SourceType,
    string? Status
);

public record SyncAvailabilityBlocksResultDto(
    int Created,
    int Updated,
    int Cancelled,
    List<string> Messages
);

public record GuardShiftCoverageRequirementDto(
    int RequirementId,
    int LocationId,
    string LocationName,
    int ScheduleId,
    string ScheduleDescription,
    byte DayOfWeek,
    string DayOfWeekName,
    int RequiredGuards,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    string? Notes
);

public record CreateCoverageRequirementDto(
    int LocationId,
    int ScheduleId,
    byte DayOfWeek,
    int RequiredGuards,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string? Notes
);

public record UpdateCoverageRequirementDto(
    byte DayOfWeek,
    int RequiredGuards,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    string? Notes
);
