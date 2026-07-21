namespace WsUtaSystem.Application.DTOs.TramiteRequirements;

public class TramiteRequirementDto
{
    public int RequirementId { get; set; }
    public int ModuleTypeId { get; set; }
    public string? ModuleTypeName { get; set; }
    public int? SpecificTypeId { get; set; }
    public int DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TramiteRequirementCreateDto
{
    public int ModuleTypeId { get; set; }
    public int? SpecificTypeId { get; set; }
    public int DocumentTypeId { get; set; }
    public bool IsRequired { get; set; }
}

public class TramiteRequirementUpdateDto
{
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Módulo (ref_Types ACCESS_MODULE_TYPE) que el usuario actual puede parametrizar.</summary>
public class AccessibleModuleDto
{
    public int ModuleTypeId { get; set; }
    public string ModuleTypeName { get; set; } = null!;
    public string? ModuleTypeDescription { get; set; }
}
