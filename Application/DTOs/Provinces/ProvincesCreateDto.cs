namespace WsUtaSystem.Application.DTOs.Provinces;
public class ProvincesCreateDto
{
    //public class Provinces { get; set; }
    public string ProvinceId { get; set; }
    public string CountryId { get; set; }
    public string ProvinceCode { get; set; }
    public string ProvinceName { get; set; }
    public DateTime CreatedAt { get; set; }
}
