namespace WsUtaSystem.Application.DTOs.Guards;

/// <summary>Empleado con cargo de guardia (ver GuardRotationGroupService.GuardJobNames), para el
/// buscador de "Agregar guardias" — filtrado, a diferencia del buscador genérico de empleados.</summary>
public record EligibleEmployeeDto(
    int EmployeeID,
    string FullName,
    string? IdCard,
    string? Email
);

public record GuardRotationGroupDto(
    int GroupId,
    string? GroupCode,
    string Name,
    string? Description,
    bool IsActive,
    int EmployeeCount,
    int? ParentGroupId,
    string? ParentGroupName,
    string? GroupLevelTypeName,
    string? ColorCode,
    int SubgroupCount,
    bool IsSpecial
);

public record GuardRotationGroupWithSubgroupsDto(
    int GroupId,
    string? GroupCode,
    string Name,
    string? Description,
    bool IsActive,
    string? ColorCode,
    string? GroupLevelTypeName,
    int EmployeeCount,
    int SubgroupCount,
    List<GuardRotationGroupDto> Subgroups,
    bool IsSpecial
);

public record LocationSummaryDto(
    string LocationKey,
    string LocationName,
    int TotalGroups,
    int TotalActiveGroups,
    int TotalEmployees,
    int TotalPatterns
);

public record LocationGroupDetailDto(
    int GroupId,
    string? GroupCode,
    string GroupName,
    string? Description,
    bool IsActive,
    int? PatternId,
    string? PatternCode,
    string? PatternName,
    string? PatternSequence,
    string? PatternReadable,
    int AssignedEmployees,
    bool IsSpecial
);

public record CreateGuardRotationGroupDto(
    string? GroupCode,
    string Name,
    string? Description,
    int? ParentGroupId,
    int? GroupLevelTypeId,
    string? ColorCode,
    bool IsSpecial
);

public record UpdateGuardRotationGroupDto(
    string? GroupCode,
    string Name,
    string? Description,
    bool IsActive,
    int? ParentGroupId,
    int? GroupLevelTypeId,
    string? ColorCode,
    bool IsSpecial
);

/// <summary>Crea un grupo nuevo copiando configuración (y empleados activos) de un grupo base.</summary>
public record DuplicateGuardRotationGroupDto(
    string NewName,
    string? NewGroupCode,
    int? ParentGroupIdOverride
);

public record GuardRotationGroupEmployeeDto(
    int GroupEmployeeId,
    int GroupId,
    string GroupName,
    int EmployeeId,
    string EmployeeFullName,
    string? EmployeeIdCard,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    string? Notes
);

public record AssignEmployeeToRotationGroupDto(
    int EmployeeId,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string? Notes
);

public record RemoveEmployeeFromRotationGroupDto(
    int GroupEmployeeId,
    DateOnly ValidTo
);

public record GuardGroupRotationPatternDto(
    int GroupPatternId,
    int GroupId,
    int PatternId,
    string? PatternName,
    string? PatternCode,
    DateOnly StartCycleDate,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    string? Notes
);

public record AssignPatternToGroupDto(
    int PatternId,
    DateOnly StartCycleDate,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string? Notes
);
