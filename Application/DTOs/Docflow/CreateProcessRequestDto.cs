namespace WsUtaSystem.Application.DTOs.Docflow
{
    /// <summary>
    /// DTO para la solicitud de creación de un nuevo proceso.
    /// </summary>
    public class CreateProcessRequestDto
    {
        /// <summary>
        /// Nombre del nuevo proceso.
        /// </summary>
        public string ProcessName { get; set; } = null!;

        /// <summary>
        /// Código único del proceso (opcional, se puede generar automáticamente).
        /// </summary>
        public string? ProcessCode { get; set; }

        /// <summary>
        /// Descripción del proceso (opcional).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Identificador del proceso padre (null si es un macro proceso).
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Identificador del departamento responsable.
        /// </summary>
        public int ResponsibleDepartmentId { get; set; }

        /// <summary>
        /// Indica si el proceso está activo.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
