using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.Trainings;

public class TrainingWithDocumentResultDto
{
    public TrainingsDto Training { get; set; } = null!;
    public StoredFileDto? StoredFile { get; set; }
}
