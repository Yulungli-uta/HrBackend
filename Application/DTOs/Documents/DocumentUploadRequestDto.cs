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

        /// <summary>
        /// Opcional. Rol del archivo dentro de un contrato o una acción de personal: "signed" (documento firmado), "attachment" (anexo),
        /// "cancellation" (oficio de anulación) o "resignation" (renuncia). Sin valor se asume anexo (o el rol que indique el tipo de documento).
        /// </summary>
        public string? DocumentRole { get; set; }

        /// <summary>Número de resolución/oficio (opcional), aplicado a todos los archivos del lote.</summary>
        public string? DocumentReferenceNumber { get; set; }

        /// <summary>Fecha de la resolución/oficio (opcional), aplicada a todos los archivos del lote.</summary>
        public DateOnly? DocumentReferenceDate { get; set; }

        public List<IFormFile> Files { get; set; } = new();
    }
}
