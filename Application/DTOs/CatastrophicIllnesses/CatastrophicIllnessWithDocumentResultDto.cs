using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.CatastrophicIllnesses;

public class CatastrophicIllnessWithDocumentResultDto
{
    public CatastrophicIllnessesDto CatastrophicIllness { get; set; } = null!;
    public StoredFileDto? StoredFile { get; set; }
}
