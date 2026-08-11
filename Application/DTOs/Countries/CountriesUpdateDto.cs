namespace WsUtaSystem.Application.DTOs.Countries;
public class CountriesUpdateDto
{
    //public class Countries { get; set; }
    public string CountryId { get; set; } = null!;
    public string? CountryCode { get; set; }
    public string CountryName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
