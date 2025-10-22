using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.FileManagement;

public class FileUploadMultipleRequestDto
{
    public string DirectoryCode { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public List<IFormFile> Files { get; set; } = new();
}

