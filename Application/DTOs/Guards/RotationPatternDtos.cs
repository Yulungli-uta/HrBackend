namespace WsUtaSystem.Application.DTOs.Guards;

public record RotationPatternDto(
    int PatternId,
    string? PatternCode,
    string Name,
    string? Description,
    int PatternTypeId,
    string? PatternTypeName,
    int CycleDays,
    bool IsActive,
    List<RotationPatternDetailDto> Details
);

public record CreateRotationPatternDto(
    string? PatternCode,
    string Name,
    string? Description,
    int PatternTypeId,
    int CycleDays,
    List<CreateRotationPatternDetailDto> Details
);

public record UpdateRotationPatternDto(
    string? PatternCode,
    string Name,
    string? Description,
    int PatternTypeId,
    int CycleDays,
    bool IsActive
);

public record RotationPatternDetailDto(
    int PatternDetailId,
    int PatternId,
    int DayOrder,
    int? ScheduleId,
    string? ScheduleDescription,
    string? ScheduleCode,
    bool IsRestDay,
    string? Notes
);

public record CreateRotationPatternDetailDto(
    int DayOrder,
    int? ScheduleId,
    bool IsRestDay,
    string? Notes
);

public record UpsertRotationPatternDetailsDto(
    List<CreateRotationPatternDetailDto> Details
);
