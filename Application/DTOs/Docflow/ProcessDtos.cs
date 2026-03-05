namespace WsUtaSystem.Application.DTOs.Docflow
{
    public class ProcessDto
    {
        /// <summary>
        /// Identificador único del proceso.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Identificador del proceso padre (null si es un macro proceso).
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Código único del proceso (ej: CONT, CONT-FAC).
        /// </summary>
        public string ProcessCode { get; set; } = null!;

        /// <summary>
        /// Nombre del proceso.
        /// </summary>
        public string ProcessName { get; set; } = null!;

        /// <summary>
        /// Descripción del proceso (opcional).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Identificador del departamento responsable de este proceso.
        /// </summary>
        public int ResponsibleDepartmentId { get; set; }

        /// <summary>
        /// Nombre del departamento responsable (para mostrar en UI).
        /// </summary>
        public string? ResponsibleDepartmentName { get; set; }

        /// <summary>
        /// Indica si el proceso está activo.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Procesos hijos (sub-procesos) de este proceso.
        /// </summary>
        public List<ProcessDto>? Children { get; set; }

        /// <summary>
        /// Fecha de creación.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Fecha de última actualización.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }

    //public sealed class DocumentRuleDto
    //{
    //    public int RuleId { get; set; }
    //    public int ProcessId { get; set; }
    //    public string DocumentType { get; set; } = null!;
    //    public bool IsRequired { get; set; }
    //    public byte DefaultVisibility { get; set; }
    //    public bool AllowVisibilityOverride { get; set; }
    //}

    // ============================================================
    // Dynamic fields schema (por proceso)
    // ============================================================

    /// <summary>
    /// Schema de un campo dinámico. Se guarda en ProcessHierarchy.DynamicFieldMetadata como JSON array.
    /// </summary>
    public sealed class DynamicFieldSchemaDto
    {
        public required string Name { get; set; }       // clave técnica del campo (única por proceso)
        public required string Type { get; set; }       // string|number|boolean|date
        public object? Value { get; set; }              // valor por defecto (para inicializar UI)
        public bool Required { get; set; }              // obligatorio
    }

    /// <summary>
    /// DTO de respuesta para leer schema de proceso.
    /// </summary>
    public sealed class ProcessDynamicFieldsDto
    {
        public int ProcessId { get; set; }
        public List<DynamicFieldSchemaDto> DynamicFieldMetadata { get; set; } = new();
    }
}
