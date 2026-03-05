namespace WsUtaSystem.Application.DTOs.Docflow
{
    /// <summary>
    /// DTO para la solicitud de creación de una nueva regla de documento.
    /// </summary>
    public class CreateDocumentRuleRequestDto
    {
        /// <summary>
        /// Tipo de documento (ej: Contrato Laboral, Factura Original).
        /// </summary>
        public string DocumentType { get; set; } = null!;

        /// <summary>
        /// Indica si este documento es obligatorio.
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Número mínimo de archivos requeridos.
        /// </summary>
        public int MinFiles { get; set; } = 1;

        /// <summary>
        /// Número máximo de archivos permitidos.
        /// </summary>
        public int MaxFiles { get; set; } = 1;

        /// <summary>
        /// Visibilidad por defecto (1 = PUBLIC_WITHIN_CASE, 2 = PRIVATE_TO_UPLOADER_DEPT).
        /// </summary>
        public int DefaultVisibility { get; set; } = 1;

        /// <summary>
        /// Permite cambiar la visibilidad al cargar.
        /// </summary>
        public bool AllowVisibilityOverride { get; set; } = true;
    }
}
