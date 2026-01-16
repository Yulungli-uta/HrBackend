namespace WsUtaSystem.Application.DTOs.StoredFile
{
    public class StoredFileUpdateDto
    {
        public string? OriginalFileName { get; set; }
        public byte? Status { get; set; }
        public string? ContentType { get; set; }
        public int DocumentTypeId { get; set; }
    }
}
