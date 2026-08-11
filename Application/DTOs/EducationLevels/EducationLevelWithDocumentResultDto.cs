using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.EducationLevels;

public class EducationLevelWithDocumentResultDto
{
    public EducationLevelsDto EducationLevel { get; set; } = null!;
    public StoredFileDto? StoredFile { get; set; }
}
