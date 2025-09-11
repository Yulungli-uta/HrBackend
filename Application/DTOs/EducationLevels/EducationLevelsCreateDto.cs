namespace WsUtaSystem.Application.DTOs.EducationLevels;
public class EducationLevelsCreateDto
{
    ////public class EducationLevels { get; set; }
    public int EducationId { get; set; }
    public int PersonId { get; set; }
    public int EducationLevelTypeId { get; set; }
    public int InstitutionId { get; set; }
    public string Title { get; set; }
    public string Specialty { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Grade { get; set; }
    public string Location { get; set; }
    public decimal Score { get; set; }
    public DateTime CreatedAt { get; set; }
}
