namespace WsUtaSystem.Application.DTOs.WorkExperiences;
public class WorkExperiencesDto
{
    //public class WorkExperiences { get; set; }
    public int WorkExpId { get; set; }
    public int PersonId { get; set; }
    public string CountryId { get; set; }
    public string Company { get; set; }
    public int InstitutionTypeId { get; set; }
    public string EntryReason { get; set; }
    public string? ExitReason { get; set; }
    public string Position { get; set; }
    public string? InstitutionAddress { get; set; }
    public DateOnly StartDate { get; set; }
    // Nullable: sin esto, AutoMapper convertía un EndDate real (NULL, "Trabajo actual")
    // en DateOnly.MinValue ("0001-01-01") al responder el GET — ese valor truthy volvía a
    // entrar al formulario de edición y contaminaba el payload del Update (causa real
    // encontrada en vivo de la observación 26, junto con el mismo bug ya corregido en
    // WorkExperiencesUpdateDto.EndDate).
    public DateOnly? EndDate { get; set; }
    public int ExperienceTypeId { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
}
