namespace WsUtaSystem.Models; 
public class Provinces
{
    public string ProvinceId { get; set; } = null!;          // Cambiado a string (VARCHAR)
    public string CountryId { get; set; } = null!;           // Cambiado a string (VARCHAR)
    public string ProvinceName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
