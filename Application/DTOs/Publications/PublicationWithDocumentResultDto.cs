using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.Publications;

public class PublicationWithDocumentResultDto
{
    public PublicationsDto Publication { get; set; } = null!;
    public StoredFileDto? StoredFile { get; set; }
}
