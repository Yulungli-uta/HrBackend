namespace WsUtaSystem.Application.DTOs.People;
public class PeopleUpdateDto
{
    //public class People { get; set; }
    public int PersonId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int IdentType { get; set; }
    public string IdCard { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateOnly BirthDate { get; set; }
    public int Sex { get; set; }
    public int Gender { get; set; }
    public string? Disability { get; set; }
    public string Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MaritalStatusTypeId { get; set; }
    public string MilitaryCard { get; set; }
    public string? MotherName { get; set; }
    public string? FatherName { get; set; }
    public int CountryId { get; set; }
    public int ProvinceId { get; set; }
    public int CantonId { get; set; }
    public int? YearsOfResidence { get; set; }
    public int EthnicityTypeId { get; set; }
    public int BloodTypeTypeId { get; set; }
    public int? SpecialNeedsTypeId { get; set; }
    public decimal? DisabilityPercentage { get; set; }
    public string? ConadisCard { get; set; }
}
