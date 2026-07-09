namespace WsUtaSystem.Application.DTOs.Documents
{
    /// <summary>
    /// Request unificado para subir múltiples archivos y registrarlos en DB.
    /// Se envía como multipart/form-data:
    /// - DirectoryCode
    /// - EntityType
    /// - EntityId
    /// - RelativePath (opcional)
    /// - Files (1..N)
    /// </summary>
    public class DocumentUploadRequestDto
    {
        public string DirectoryCode { get; set; } = default!;
        public string EntityType { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public string? RelativePath { get; set; }
        public int? DocumentTypeId { get; set; }

        /// <summary>Número de resolución/oficio (opcional), aplicado a todos los archivos del lote.</summary>
        public string? DocumentReferenceNumber { get; set; }

        /// <summary>Fecha de la resolución/oficio (opcional), aplicada a todos los archivos del lote.</summary>
        public DateOnly? DocumentReferenceDate { get; set; }

        public List<IFormFile> Files { get; set; } = new();
    }
}
