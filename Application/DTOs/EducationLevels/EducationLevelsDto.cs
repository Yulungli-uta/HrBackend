namespace WsUtaSystem.Application.DTOs.EducationLevels;
public class EducationLevelsDto
{
    //public class EducationLevels { get; set; }
    public int EducationId { get; set; }
    public int PersonId { get; set; }
    public int EducationLevelTypeId { get; set; }
    public int? InstitutionId { get; set; }
    public string Title { get; set; } = null!;
    public string? Specialty { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Grade { get; set; }
    public string? Location { get; set; }
    public decimal? Score { get; set; }
    public string? SenescytRegistrationNumber { get; set; }
    public int? SiiesGradoTypeId { get; set; }
    public DateOnly? SenescytGraduationDate { get; set; }
    public DateOnly? SenescytRegistrationDate { get; set; }
    public string? SenescytType { get; set; }
    public string? SenescytNivelNombreOriginal { get; set; }
    public string? InstitutionNameOriginal { get; set; }
    public string Source { get; set; } = "Manual";
    public DateTime CreatedAt { get; set; }
}
