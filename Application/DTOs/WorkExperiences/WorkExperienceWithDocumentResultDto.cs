using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.WorkExperiences;

public class WorkExperienceWithDocumentResultDto
{
    public WorkExperiencesDto WorkExperience { get; set; } = null!;
    public StoredFileDto? StoredFile { get; set; }
}
