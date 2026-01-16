namespace WsUtaSystem.Application.DTOs.StoredFile
{
    public class StoredFileDto
    {
        public int FileId { get; set; }
        public Guid FileGuid { get; set; }
        public string DirectoryCode { get; set; }

        public string EntityType { get; set; }
        public string EntityId { get; set; }

        public short UploadYear { get; set; }
        public string RelativeFolder { get; set; }
        public string StoredFileName { get; set; }
        public string? OriginalFileName { get; set; }
        public int DocumentTypeId { get; set; }

        public string? Extension { get; set; }
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }

        public byte Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Calculado en backend (NO persistido)
        public string? FullPath { get; set; }
    }
}
