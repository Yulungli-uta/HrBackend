namespace WsUtaSystem.Application.DTOs.Guards;

public record GuardServiceLocationDto(
    int LocationId,
    int? ParentLocationId,
    int? RootLocationId,
    int LocationTypeId,
    string? LocationTypeName,
    string? LocationCode,
    string LocationName,
    string? Description,
    string? LocationPath,
    int Level,
    bool RequiresCoverage,
    bool IsAssignable,
    bool IsActive,
    List<GuardServiceLocationDto>? Children
);

public record GuardServiceLocationTreeDto(
    int LocationId,
    string LocationName,
    string? LocationCode,
    int Level,
    bool IsAssignable,
    bool RequiresCoverage,
    bool IsActive,
    List<GuardServiceLocationTreeDto> Children
);

public record CreateGuardServiceLocationDto(
    int? ParentLocationId,
    int? RootLocationId,
    int LocationTypeId,
    string? LocationCode,
    string LocationName,
    string? Description,
    bool RequiresCoverage,
    bool IsAssignable
);

public record UpdateGuardServiceLocationDto(
    int LocationTypeId,
    string? LocationCode,
    string LocationName,
    string? Description,
    bool RequiresCoverage,
    bool IsAssignable,
    bool IsActive
);
