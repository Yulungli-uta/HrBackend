namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

public sealed class PersonnelActionTypeCreateDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public string NumberingPrefix { get; set; } = null!;
    public string? TemplateCode { get; set; }
    public bool IsActive { get; set; } = true;
}
