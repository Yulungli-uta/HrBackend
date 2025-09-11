namespace WsUtaSystem.Application.DTOs.WorkExperiences;
public class WorkExperiencesUpdateDto
{
    //public class WorkExperiences { get; set; }
    public int WorkExpId { get; set; }
    public int PersonId { get; set; }
    public int CountryId { get; set; }
    public string Company { get; set; }
    public int InstitutionTypeId { get; set; }
    public string EntryReason { get; set; }
    public string ExitReason { get; set; }
    public string Position { get; set; }
    public string InstitutionAddress { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int ExperienceTypeId { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
}
