namespace WsUtaSystem.Application.DTOs.Addresses;
public class AddressesDto
{
    //public class Addresses { get; set; }
    public int AddressId { get; set; }
    public int PersonId { get; set; }
    public int AddressTypeId { get; set; }
    public int CountryId { get; set; }
    public int ProvinceId { get; set; }
    public int CantonId { get; set; }
    public string Parish { get; set; }
    public string Neighborhood { get; set; }
    public string MainStreet { get; set; }
    public string SecondaryStreet { get; set; }
    public string HouseNumber { get; set; }
    public string Reference { get; set; }
    public DateTime CreatedAt { get; set; }
}
