namespace WsUtaSystem.Application.DTOs.Documents
{
    public class DocumentUploadSingleRequestDto
    {
        public string DirectoryCode { get; set; } = default!;
        public string EntityType { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public string? RelativePath { get; set; }

        public int? DocumentTypeId { get; set; }

        /// <summary>
        /// Opcional. Rol del archivo dentro de un contrato o una acción de personal: "signed" (documento firmado), "attachment" (anexo),
        /// "cancellation" (oficio de anulación) o "resignation" (renuncia). Sin valor se asume anexo (o el rol que indique el tipo de documento).
        /// </summary>
        public string? DocumentRole { get; set; }

        /// <summary>Número de resolución/oficio (opcional).</summary>
        public string? DocumentReferenceNumber { get; set; }

        /// <summary>Fecha de la resolución/oficio (opcional).</summary>
        public DateOnly? DocumentReferenceDate { get; set; }

        // Debe llamarse "File" si tu FormData usa "File"
        public IFormFile File { get; set; } = default!;
    }
}
