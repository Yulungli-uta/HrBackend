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
        public List<IFormFile> Files { get; set; } = new();
    }
}
