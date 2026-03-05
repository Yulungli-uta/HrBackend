namespace WsUtaSystem.Application.DTOs.Docflow
{


    public sealed class CreateInstanceRequest
    {
        /// <summary>ID del proceso inicial donde comienza el expediente.</summary>
        public required int InitialProcessId { get; set; }

        /// <summary>Nombre o descripción del expediente (ej: "Contratación de Juan Pérez").</summary>
        public string? InstanceName { get; set; }

        /// <summary>JSON con metadata dinámica inicial del expediente.</summary>
        public string? DynamicMetadata { get; set; }

        /// <summary>Usuario asignado al expediente (opcional).</summary>
        public int? AssignedToUserId { get; set; }
    }

    public sealed class CreateDocumentRequest
    {
        public int? RuleId { get; set; }
        public required string DocumentName { get; set; }
        public byte? Visibility { get; set; } // si null => default de la regla o PUBLIC
    }

    public sealed class CreateMovementRequest
    {
        public required string MovementType { get; set; } // FORWARD / RETURN
        public string? Comments { get; set; }
        public int? AssignedToUserId { get; set; }
    }
    // ============================================================
    // Dynamic fields schema update
    // ============================================================

    public sealed class UpdateProcessDynamicFieldsRequest
    {
        public List<DynamicFieldSchemaDto> DynamicFieldMetadata { get; set; } = new();
    }
}
