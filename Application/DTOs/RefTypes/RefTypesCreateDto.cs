namespace WsUtaSystem.Application.DTOs.RefTypes;
public class RefTypesCreateDto
{
    //public class RefTypes { get; set; }
    public int TypeId { get; set; }
    public string Category { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
