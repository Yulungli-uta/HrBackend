namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

public sealed record PersonnelActionTypeDto(
    int PersonnelActionTypeId,
    string Name,
    string Code,
    string? Description,
    string NumberingPrefix,
    int NumberingYear,
    int NumberingLastSequence,
    string? TemplateCode,
    bool IsActive
);
