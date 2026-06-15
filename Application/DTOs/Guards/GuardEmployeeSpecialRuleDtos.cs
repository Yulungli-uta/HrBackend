namespace WsUtaSystem.Application.DTOs.Guards;

// ─── Condiciones especiales por guardia ──────────────────────────────────────

public record GuardEmployeeSpecialRuleDto(
    int     SpecialRuleId,
    int     EmployeeId,
    string  EmployeeFullName,
    string? EmployeeIdCard,
    int?    FixedLocationId,
    string? FixedLocationName,
    string? FixedLocationCode,
    int?    FixedScheduleId,
    string? FixedScheduleDescription,
    string? FixedScheduleCode,
    bool    NoNightShift,
    bool    OnlyWeekDays,
    bool    WeekendPriority,
    bool    NightPriority,
    string? Reason,
    DateOnly  ValidFrom,
    DateOnly? ValidTo,
    bool    RequiresApproval,
    bool    IsActive
);

public record CreateGuardEmployeeSpecialRuleDto(
    int      EmployeeId,
    int?     FixedLocationId,
    int?     FixedScheduleId,
    bool     NoNightShift,
    bool     OnlyWeekDays,
    bool     WeekendPriority,
    bool     NightPriority,
    string?  Reason,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool     RequiresApproval
);

public record UpdateGuardEmployeeSpecialRuleDto(
    int?     FixedLocationId,
    int?     FixedScheduleId,
    bool     NoNightShift,
    bool     OnlyWeekDays,
    bool     WeekendPriority,
    bool     NightPriority,
    string?  Reason,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool     RequiresApproval,
    bool     IsActive
);
