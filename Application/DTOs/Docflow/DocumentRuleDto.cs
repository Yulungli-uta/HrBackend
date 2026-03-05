namespace WsUtaSystem.Application.DTOs.Docflow
{
    /// <summary>
    /// DTO para representar una regla de documento en un proceso de Docflow.
    /// Define qué documentos son requeridos y sus restricciones.
    /// </summary>
    public class DocumentRuleDto
    {
        /// <summary>
        /// Identificador único de la regla.
        /// </summary>
        public int RuleId { get; set; }

        /// <summary>
        /// Identificador del proceso al que aplica esta regla.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Tipo de documento (ej: Contrato Laboral, Factura Original).
        /// </summary>
        public string DocumentType { get; set; } = null!;

        /// <summary>
        /// Indica si este documento es obligatorio en el proceso.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Número mínimo de archivos requeridos para este tipo de documento.
        /// </summary>
        public int MinFiles { get; set; } = 1;

        /// <summary>
        /// Número máximo de archivos permitidos para este tipo de documento.
        /// </summary>
        public int MaxFiles { get; set; } = 1;

        /// <summary>
        /// Visibilidad por defecto del documento (1 = PUBLIC_WITHIN_CASE, 2 = PRIVATE_TO_UPLOADER_DEPT).
        /// </summary>
        public int DefaultVisibility { get; set; } = 1;

        /// <summary>
        /// Indica si se permite cambiar la visibilidad al cargar el documento.
        /// </summary>
        public bool AllowVisibilityOverride { get; set; }

        /// <summary>
        /// Fecha de creación.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Fecha de última actualización.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
