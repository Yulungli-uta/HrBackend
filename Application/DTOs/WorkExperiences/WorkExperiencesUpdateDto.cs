namespace WsUtaSystem.Application.DTOs.WorkExperiences;
public class WorkExperiencesUpdateDto
{
    //public class WorkExperiences { get; set; }
    public int WorkExpId { get; set; }
    public int PersonId { get; set; }
    public string CountryId { get; set; } = null!;
    public string Company { get; set; } = null!;
    public int InstitutionTypeId { get; set; }
    public string EntryReason { get; set; } = null!;
    public string? ExitReason { get; set; }
    public string Position { get; set; } = null!;
    public string? InstitutionAddress { get; set; }
    public DateOnly StartDate { get; set; }
    // Nullable: "Trabajo actual" (IsCurrent=true) no tiene fecha de fin. Antes era
    // DateOnly no nulable — bloqueaba el update con "One or more validation errors
    // occurred" para cualquier experiencia laboral vigente (hallazgo informe
    // UTA-DITIC-PS-027-2026, observación 26; mismo patrón que IdentType/EmployeeType).
    public DateOnly? EndDate { get; set; }
    public int ExperienceTypeId { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
}
