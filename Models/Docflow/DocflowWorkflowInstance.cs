using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Docflow
{
    /// <summary>
    /// Representa una instancia de expediente en el workflow.
    /// </summary>
    public class DocflowWorkflowInstance : IAuditable
    {
        /// <summary>Identificador único del expediente.</summary>
        public Guid InstanceId { get; set; }

        /// <summary>Identificador del proceso/etapa actual del expediente.</summary>
        public int ProcessId { get; set; }

        /// <summary>Identificador del proceso raíz (macroproceso) al que pertenece este expediente.</summary>
        public int? RootProcessId { get; set; }

        /// <summary>Nombre o descripción legible del expediente.</summary>
        public string? InstanceName { get; set; }

        /// <summary>Estado funcional del expediente (IN_PROGRESS, COMPLETED, etc.).</summary>
        public string CurrentStatus { get; set; } = null!;

        /// <summary>Departamento actual responsable del expediente.</summary>
        public int CurrentDepartmentId { get; set; }

        /// <summary>Usuario asignado al expediente (opcional).</summary>
        public int? AssignedToUserId { get; set; }

        /// <summary>JSON con metadata dinámica del expediente.</summary>
        public string? DynamicMetadata { get; set; }

        // Auditoría
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
