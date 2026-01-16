namespace WsUtaSystem.Application.DTOs.StoredFile
{
    public class StoredFileCreateDto
    {
        public string DirectoryCode { get; set; }
        public string EntityType { get; set; }       
        public string EntityId { get; set; }
        public string? StoredFileName { get; set; } = null!; // e.g., "anexo_1.pdf"
        public string? OriginalFileName { get; set; }
        public int DocumentTypeId { get; set; }

    }
}
