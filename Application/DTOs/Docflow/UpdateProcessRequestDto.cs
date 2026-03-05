namespace WsUtaSystem.Application.DTOs.Docflow
{
    /// <summary>
    /// DTO para la solicitud de actualización de un proceso existente.
    /// Todos los campos son opcionales para permitir actualizaciones parciales.
    /// </summary>
    public class UpdateProcessRequestDto
    {
        /// <summary>
        /// Nombre del proceso (opcional).
        /// </summary>
        public string? ProcessName { get; set; }

        /// <summary>
        /// Descripción del proceso (opcional).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Identificador del proceso padre (opcional).
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Identificador del departamento responsable (opcional).
        /// </summary>
        public int? ResponsibleDepartmentId { get; set; }

        /// <summary>
        /// Indica si el proceso está activo (opcional).
        /// </summary>
        public bool? IsActive { get; set; }
    }
}
