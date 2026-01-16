namespace WsUtaSystem.Application.DTOs.Documents
{
    public class DocumentListQueryDto
    {
        public string DirectoryCode { get; set; } = default!;
        public string EntityType { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public int? UploadYear { get; set; }
        public int? Status { get; set; } = 1;
    }
}
