namespace WsUtaSystem.Application.DTOs.Cantons;
public class CantonsCreateDto
{
    //public class Cantons { get; set; }
    public string CantonId { get; set; } = null!;
    public string ProvinceId { get; set; } = null!;
    public string? CantonCode { get; set; }
    public string CantonName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
