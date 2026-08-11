namespace WsUtaSystem.Application.DTOs.Cantons;
public class CantonsUpdateDto
{
    //public class Cantons { get; set; }
    public string CantonId { get; set; } = null!;
    public string ProvinceId { get; set; } = null!;
    public string? CantonCode { get; set; }
    public string CantonName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
