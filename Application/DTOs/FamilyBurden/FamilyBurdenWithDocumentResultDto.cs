using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.FamilyBurden;

public class FamilyBurdenWithDocumentResultDto
{
    public FamilyBurdenDto FamilyBurden { get; set; } = null!;
    public StoredFileDto? StoredFile { get; set; }
}
