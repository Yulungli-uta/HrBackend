namespace WsUtaSystem.Application.DTOs.FileManagement
{
    public class FileUploadResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string FullPath { get; set; } = null!;
        public string RelativePath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public long FileSize { get; set; }
        public int Year { get; set; }
    }
}

