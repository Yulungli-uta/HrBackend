using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.Languages;

public class LanguageWithDocumentResultDto
{
    public LanguagesDto Language { get; set; } = null!;
    public StoredFileDto? StoredFile { get; set; }
}
