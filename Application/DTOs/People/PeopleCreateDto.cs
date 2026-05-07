namespace WsUtaSystem.Application.DTOs.People;

public class PeopleCreateDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int IdentType { get; set; }
    public string IdCard { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public DateOnly? BirthDate { get; set; }
    public int? Sex { get; set; }
    public int? Gender { get; set; }
    public string? Disability { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public int? MaritalStatusTypeId { get; set; }
    public string? MilitaryCard { get; set; }
    public string? MotherName { get; set; }
    public string? FatherName { get; set; }
    public string? CountryId { get; set; }
    public string? ProvinceId { get; set; }
    public string? CantonId { get; set; }
    public int? YearsOfResidence { get; set; }
    public int? EthnicityTypeId { get; set; }
    public int? BloodTypeTypeId { get; set; }
    public int? SpecialNeedsTypeId { get; set; }
    public decimal? DisabilityPercentage { get; set; }
    public string? ConadisCard { get; set; }
}
