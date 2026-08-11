namespace WsUtaSystem.Application.DTOs.Institutions;
public class InstitutionsUpdateDto
{
    //public class Institutions { get; set; }
    public int InstitutionId { get; set; }
    public string Name { get; set; } = null!;
    public int InstitutionTypeId { get; set; }
    public string CountryId { get; set; } = null!;
    public string ProvinceId { get; set; } = null!;
    public string CantonId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
