namespace WsUtaSystem.Application.DTOs.Addresses;
public class AddressesUpdateDto
{
    public int AddressId { get; set; }
    public int PersonId { get; set; }
    public int AddressTypeId { get; set; }
    public string CountryId { get; set; } = null!;
    public string ProvinceId { get; set; } = null!;
    public string CantonId { get; set; } = null!;
    public string? Parish { get; set; }
    public string? Neighborhood { get; set; }
    public string MainStreet { get; set; } = null!;
    public string? SecondaryStreet { get; set; }
    public string? HouseNumber { get; set; }
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; }
}
