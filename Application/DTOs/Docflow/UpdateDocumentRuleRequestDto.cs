namespace WsUtaSystem.Application.DTOs.Docflow
{
    /// <summary>
    /// DTO para la solicitud de actualización de una regla de documento existente.
    /// Todos los campos son opcionales para permitir actualizaciones parciales.
    /// </summary>
    public class UpdateDocumentRuleRequestDto
    {
        /// <summary>
        /// Tipo de documento (opcional).
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// Indica si es obligatorio (opcional).
        /// </summary>
        public bool? IsRequired { get; set; }

        /// <summary>
        /// Número mínimo de archivos (opcional).
        /// </summary>
        public int? MinFiles { get; set; }

        /// <summary>
        /// Número máximo de archivos (opcional).
        /// </summary>
        public int? MaxFiles { get; set; }

        /// <summary>
        /// Visibilidad por defecto (opcional).
        /// </summary>
        public int? DefaultVisibility { get; set; }

        /// <summary>
        /// Permite cambiar visibilidad (opcional).
        /// </summary>
        public bool? AllowVisibilityOverride { get; set; }
    }
}
