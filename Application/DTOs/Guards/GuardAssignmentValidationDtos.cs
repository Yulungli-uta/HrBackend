namespace WsUtaSystem.Application.DTOs.Guards;

public record GuardAssignmentValidationDto(
    long ValidationId,
    int EmployeeId,
    string EmployeeFullName,
    int? PlanningId,
    int? ShiftChangeId,
    string ValidationType,
    string Result,
    string Severity,
    DateTime ValidationDate,
    string Message,
    string? Details
);

public record ValidateGuardAssignmentRequestDto(
    int EmployeeId,
    int LocationId,
    DateOnly WorkDate,
    int ScheduleId,
    int? PlanningId,
    bool AllowDoubleShiftOverride
);

public record ValidateGuardAssignmentResultDto(
    bool CanAssign,
    bool HasBlockingErrors,
    bool HasWarnings,
    List<GuardAssignmentValidationDto> Validations
);

public record GuardDashboardDto(
    int TodayShiftsCount,
    int UncoveredPostsCount,
    int PendingReplacementsCount,
    int EmployeesWithPermissionOrVacationCount,
    int DoubleShiftAlertsCount,
    List<GuardShiftCalendarItemDto> TodayShifts,
    List<GuardShiftChangeDto> PendingReplacements
);
