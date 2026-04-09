using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.ScheduleChange
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  REQUEST DTOs  (entrada → backend)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea una nueva planificación de cambio de horario.
    /// Solo puede ser invocado por el jefe inmediato de los empleados incluidos.
    /// </summary>
    public sealed record CreateScheduleChangePlanRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; init; } = string.Empty;

        [MaxLength(1000)]
        public string? Justification { get; init; }

        /// <summary>EmployeeID del jefe inmediato que solicita el cambio.</summary>
        [Required]
        public int RequestedByBossID { get; init; }

        /// <summary>ScheduleID del nuevo horario a aplicar.</summary>
        [Required]
        public int NewScheduleID { get; init; }

        /// <summary>Fecha deseada de inicio del nuevo horario.</summary>
        [Required]
        public DateOnly EffectiveDate { get; init; }

        /// <summary>Horas de anticipación requeridas. Solo 24 o 48.</summary>
        //[Required]
        //[Range(24, 48)]
        public byte ApplyAfterHours { get; init; }

        public bool IsPermanent { get; init; } = true;

        /// <summary>Obligatorio cuando IsPermanent es false.</summary>
        public DateOnly? TemporalEndDate { get; init; }

        /// <summary>Lista de empleados a incluir en el plan. Mínimo 1.</summary>
        [Required]
        [MinLength(1)]
        public List<CreateScheduleChangePlanDetailRequest> Details { get; init; } = [];
    }

    /// <summary>Empleado individual dentro del request de creación del plan.</summary>
    public sealed record CreateScheduleChangePlanDetailRequest
    {
        [Required]
        public int EmployeeID { get; init; }

        [MaxLength(500)]
        public string? Notes { get; init; }
    }

    // ───────────────────────────────────────────────────────────────────────────

    /// <summary>Aprueba o rechaza un plan pendiente.</summary>
    public sealed record ApproveScheduleChangePlanRequest
    {
        [Required]
        public int PlanID { get; init; }

        /// <summary>EmployeeID del aprobador.</summary>
        [Required]
        public int ApprovedByID { get; init; }

        [Required]
        public bool IsApproved { get; init; }

        /// <summary>Obligatorio cuando IsApproved es false.</summary>
        [MaxLength(500)]
        public string? RejectionReason { get; init; }
    }

    // ───────────────────────────────────────────────────────────────────────────

    /// <summary>Cancela un plan que aún no ha sido ejecutado.</summary>
    public sealed record CancelScheduleChangePlanRequest
    {
        [Required]
        public int PlanID { get; init; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; init; } = string.Empty;
    }


    // ═══════════════════════════════════════════════════════════════════════════
    //  RESPONSE DTOs  (salida → cliente)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Resumen del plan para listados y cabecera de detalle.</summary>
    public sealed record ScheduleChangePlanResponse
    {
        public int PlanID { get; init; }
        public string? PlanCode { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Justification { get; init; }

        public int RequestedByBossID { get; init; }
        public string? BossFullName { get; init; }

        public int NewScheduleID { get; init; }
        public string? NewScheduleDescription { get; init; }

        public DateOnly EffectiveDate { get; init; }
        public byte ApplyAfterHours { get; init; }

        /// <summary>Fecha/hora calculada en la que el SP aplicará el cambio.</summary>
        public DateTime? EffectiveApplyDate { get; init; }

        public bool IsPermanent { get; init; }
        public DateOnly? TemporalEndDate { get; init; }

        public int StatusTypeID { get; init; }
        public string? StatusName { get; init; }

        public int? ApprovedByID { get; init; }
        public string? ApprovedByFullName { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public string? RejectionReason { get; init; }

        public DateTime? AppliedAt { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }

        public List<ScheduleChangePlanDetailResponse> Details { get; init; } = [];
    }

    // ───────────────────────────────────────────────────────────────────────────

    /// <summary>Detalle de un empleado dentro de la respuesta del plan.</summary>
    public sealed record ScheduleChangePlanDetailResponse
    {
        public int DetailID { get; init; }
        public int PlanID { get; init; }

        public int EmployeeID { get; init; }
        public string? EmployeeFullName { get; init; }
        public string? EmployeeCode { get; init; }

        public int? PreviousScheduleID { get; init; }
        public string? PreviousScheduleDescription { get; init; }

        public int? PreviousEmpScheduleID { get; init; }
        public int? AppliedEmpScheduleID { get; init; }

        public int StatusTypeID { get; init; }
        public string? StatusName { get; init; }

        public string? Notes { get; init; }
        public string? OmissionReason { get; init; }

        public DateTime? AppliedAt { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    // ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resultado devuelto por el SP usp_ExecuteScheduleChangePlans.
    /// Mapea la tabla retornada por el stored procedure.
    /// </summary>
    public sealed record ScheduleChangePlanExecutionLogResponse
    {
        public int PlanID { get; init; }
        public int EmployeeID { get; init; }

        /// <summary>APLICADO | OMITIDO | ERROR</summary>
        public string Action { get; init; } = string.Empty;

        public string? Message { get; init; }
        public int? NewEmpSchedID { get; init; }
        public DateTime ExecutedAt { get; init; }
    }
}
