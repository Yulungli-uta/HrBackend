namespace WsUtaSystem.Application.DTOs.Provinces;
public class ProvincesCreateDto
{
    //public class Provinces { get; set; }
    public string ProvinceId { get; set; } = null!;
    public string CountryId { get; set; } = null!;
    public string? ProvinceCode { get; set; }
    public string ProvinceName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
