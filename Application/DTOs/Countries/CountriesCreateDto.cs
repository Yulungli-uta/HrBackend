namespace WsUtaSystem.Application.DTOs.Countries;
public class CountriesCreateDto
{
    //public class Countries { get; set; }
    public string CountryId { get; set; } = null!;
    public string? CountryCode { get; set; }
    public string CountryName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
