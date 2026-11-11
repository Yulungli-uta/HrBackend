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
    int? SiiesRelacionIesTypeId = null,
    // 2026-11-11: tipos padre/hijos (series con prefijo propio) y numeración por tipo
    int? ParentTypeId = null,
    string? NumberingFormat = null,
    bool NumberingResetsYearly = true
);
