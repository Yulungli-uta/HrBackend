namespace WsUtaSystem.Application.DTOs.Institutions;
public class InstitutionsUpdateDto
{
    //public class Institutions { get; set; }
    public int InstitutionId { get; set; }
    public string Name { get; set; }
    public int InstitutionTypeId { get; set; }
    public int CountryId { get; set; }
    public int ProvinceId { get; set; }
    public int CantonId { get; set; }
    public DateTime CreatedAt { get; set; }
}
