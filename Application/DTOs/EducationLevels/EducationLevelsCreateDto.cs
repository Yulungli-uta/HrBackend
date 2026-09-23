namespace WsUtaSystem.Application.DTOs.EducationLevels;
public class EducationLevelsCreateDto
{
    ////public class EducationLevels { get; set; }
    public int EducationId { get; set; }
    public int PersonId { get; set; }
    public int EducationLevelTypeId { get; set; }
    public int? InstitutionId { get; set; }
    public string Title { get; set; } = null!;
    public string? Specialty { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Grade { get; set; }
    public decimal? Score { get; set; }
    public string? SenescytRegistrationNumber { get; set; }
    /// <summary>Solo aplica cuando EducationLevelTypeId es Cuarto Nivel.</summary>
    public int? SiiesGradoTypeId { get; set; }
    public DateOnly? SenescytGraduationDate { get; set; }
    public DateOnly? SenescytRegistrationDate { get; set; }
    public string? SenescytType { get; set; }
    public string? CountryOfStudyId { get; set; }
    public int? UnescoSubareaTypeId { get; set; }
    public DateTime CreatedAt { get; set; }
}
