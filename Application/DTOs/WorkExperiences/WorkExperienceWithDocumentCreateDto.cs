using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.WorkExperiences;

/// <summary>DTO multipart para crear una experiencia laboral junto con su documento de respaldo.</summary>
public class WorkExperienceWithDocumentCreateDto
{
    public int PersonId { get; set; }
    public string CountryId { get; set; } = null!;
    public string Company { get; set; } = null!;
    public int InstitutionTypeId { get; set; }
    public string EntryReason { get; set; } = null!;
    public string? ExitReason { get; set; }
    public string Position { get; set; } = null!;
    public string? InstitutionAddress { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int ExperienceTypeId { get; set; }
    public bool IsCurrent { get; set; }

    public IFormFile? File { get; set; }
    public int? DocumentTypeId { get; set; }
}
