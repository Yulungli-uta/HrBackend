namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

public sealed record PersonnelActionTypeDto(
    int PersonnelActionTypeId,
    string Name,
    string Code,
    string? Description,
    string NumberingPrefix,
    int NumberingYear,
    int NumberingLastSequence,
    int? DefaultTemplateId,
    bool IsActive,
    bool RequiresAdUserCreation,
    bool RequiresAdUserDisable,
    bool RequiresAdGroupAssignment,
    string? ActionCategory,
    bool ReachesVigente,
    int? SiiesRelacionIesTypeId = null
);
