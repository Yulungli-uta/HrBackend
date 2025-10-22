namespace WsUtaSystem.Application.DTOs.FileManagement;

public class FileDeleteResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

