namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

public sealed class PersonnelActionTypeCreateDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public string NumberingPrefix { get; set; } = null!;
    public int? DefaultTemplateId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresAdUserCreation { get; set; } = false;
    public bool RequiresAdUserDisable { get; set; } = false;
    public bool RequiresAdGroupAssignment { get; set; } = false;
    public string? ActionCategory { get; set; }
    public int? SiiesRelacionIesTypeId { get; set; }
}
