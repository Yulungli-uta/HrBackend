namespace WsUtaSystem.Application.DTOs.Documents
{
    public class DocumentUploadSingleRequestDto
    {
        public string DirectoryCode { get; set; } = default!;
        public string EntityType { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public string? RelativePath { get; set; }

        public int? DocumentTypeId { get; set; }

        /// <summary>Número de resolución/oficio (opcional).</summary>
        public string? DocumentReferenceNumber { get; set; }

        /// <summary>Fecha de la resolución/oficio (opcional).</summary>
        public DateOnly? DocumentReferenceDate { get; set; }

        // Debe llamarse "File" si tu FormData usa "File"
        public IFormFile File { get; set; } = default!;
    }
}
