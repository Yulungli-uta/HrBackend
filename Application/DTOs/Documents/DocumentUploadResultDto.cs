using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.Documents
{
    public class DocumentUploadItemResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public string OriginalFileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }

        /// <summary>Si Success=true, contiene el registro DB creado.</summary>
        public StoredFileDto? StoredFile { get; set; }

        /// <summary>Si se alcanzó a subir físicamente, se devuelve para auditoría/debug.</summary>
        public string? PhysicalRelativePath { get; set; }
    }

    public class DocumentUploadResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public int Total { get; set; }
        public int Uploaded { get; set; }
        public int Failed { get; set; }

        public List<DocumentUploadItemResultDto> Items { get; set; } = new();
    }
}
