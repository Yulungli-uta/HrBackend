namespace WsUtaSystem.Application.DTOs.RefTypes;
public class RefTypesCreateDto
{
    //public class RefTypes { get; set; }
    public int TypeId { get; set; }
    public string Category { get; set; }
    public string Name { get; set; }
    // Nullable a propósito -- ver RefTypesUpdateDto.cs (mismo hallazgo, obs. 42-43).
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SiiesLabel { get; set; }
    public int SortOrder { get; set; }
}
