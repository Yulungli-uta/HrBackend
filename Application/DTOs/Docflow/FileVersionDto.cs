namespace WsUtaSystem.Application.DTOs.Docflow
{
    /// <summary>
    /// DTO que representa una versión de archivo en Docflow.
    /// Contiene información sobre una versión específica de un documento.
    /// </summary>
    public class FileVersionDto 
    {
        /// <summary>
        /// Identificador único de la versión del archivo.
        /// </summary>
        public Guid VersionId { get; set; }

        /// <summary>
        /// Identificador del documento al que pertenece esta versión.
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Número de versión incremental (1, 2, 3, ...).
        /// </summary>
        public int VersionNumber { get; set; }

        /// <summary>
        /// Extensión del archivo (ej: .pdf, .docx, .xlsx).
        /// </summary>
        public string? FileExtension { get; set; }

        /// <summary>
        /// Tamaño del archivo en bytes.
        /// </summary>
        public long FileSizeInBytes { get; set; }

        /// <summary>
        /// Fecha y hora de creación de la versión.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Identificador del usuario que subió esta versión.
        /// </summary>
        public int? CreatedBy { get; set; }

        /// <summary>
        /// Nombre del usuario que subió esta versión (para mostrar en UI).
        /// </summary>
        public string? CreatedByName { get; set; }
    }
}
