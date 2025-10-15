using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.FileManagement
{
    public class FileUploadRequestDto
    {
        public string DirectoryCode { get; set; } = null!;
        public string RelativePath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public IFormFile File { get; set; } = null!;
    }
}

