namespace WsUtaSystem.Application.DTOs.Docflow
{
    /// <summary>
    /// DTO para la solicitud de subida de archivo a un documento en Docflow.
    /// </summary>
    public class DocumentUploadRequestDto
    {
        /// <summary>
        /// Identificador del documento en Docflow.
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Identificador de la instancia (expediente) a la que pertenece el documento.
        /// </summary>
        public Guid InstanceId { get; set; }

        /// <summary>
        /// El archivo a subir (se recibe como multipart/form-data en el controlador).
        /// </summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// Comentario o descripción de la versión (opcional).
        /// </summary>
        public string? Comments { get; set; }
    }

}
