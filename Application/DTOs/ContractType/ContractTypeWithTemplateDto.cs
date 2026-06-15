namespace WsUtaSystem.Application.DTOs.ContractType;

/// <summary>DTO de tipo de contrato con información de la plantilla documental por defecto.</summary>
public sealed record ContractTypeWithTemplateDto(
    int ContractTypeId,
    string Name,
    string? Description,
    string Status,
    string? ContractCode,
    int? DocumentTemplateTypeId,
    int? DefaultTemplateId,
    string? DefaultTemplateName,
    string? DefaultTemplateCode,
    string? DefaultTemplateVersion,
    string? NumberingPrefix,
    int NumberingYear,
    int NumberingLastSequence,
    bool RequiresAdUserCreation = false,
    bool RequiresAdUserDisable = false,
    bool RequiresAdGroupAssignment = false
);

/// <summary>Respuesta con el siguiente número de documento reservado para un tipo de contrato.</summary>
public sealed record ContractNextNumberDto(
    string DocumentNumber,
    string Prefix,
    int Year,
    int Sequence
);

/// <summary>Request para asignar la plantilla por defecto a un tipo de contrato.</summary>
public sealed record SetDefaultTemplateRequest(int? TemplateId);
