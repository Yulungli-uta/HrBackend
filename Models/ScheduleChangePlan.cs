using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class ScheduleChangePlan : IAuditable
    {
      
        public int PlanID { get; set; }

        /// <summary>
        /// Código legible generado por el backend (ej. SCP-000001).
        /// </summary>
        [MaxLength(20)]
        public string? PlanCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Justification { get; set; }

        // ── Relaciones ──────────────────────────────────────────────────────

        /// <summary>FK → tbl_Employees: jefe inmediato que crea el plan.</summary>
        public int RequestedByBossID { get; set; }

        /// <summary>FK → tbl_Schedules: horario destino a aplicar.</summary>
        public int NewScheduleID { get; set; }

        // ── Temporalidad ────────────────────────────────────────────────────

        /// <summary>Fecha deseada de inicio del nuevo horario.</summary>
        public DateOnly EffectiveDate { get; set; }

        /// <summary>
        /// Horas de anticipación requeridas antes de aplicar el cambio.
        /// Solo acepta valores 24 o 48 (validado en BD y capa de aplicación).
        /// </summary>
        public byte ApplyAfterHours { get; set; }

        /// <summary>
        /// Columna calculada persistida en BD: EffectiveDate + ApplyAfterHours.
        /// Solo lectura desde el backend.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? EffectiveApplyDate { get; set; }

        /// <summary>
        /// true  = el cambio es permanente (ValidTo = NULL en EmployeeSchedules).
        /// false = cambio temporal; requiere <see cref="TemporalEndDate"/>.
        /// </summary>
        public bool IsPermanent { get; set; } = true;

        /// <summary>Fecha de fin cuando el cambio es temporal.</summary>
        public DateOnly? TemporalEndDate { get; set; }

        // ── Estado (FK → ref_Types categoria SCHEDULE_CHANGE_STATUS) ───────

        public int StatusTypeID { get; set; }

        // ── Aprobación ──────────────────────────────────────────────────────

        public int? ApprovedByID { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // ── Ejecución ───────────────────────────────────────────────────────

        public DateTime? AppliedAt { get; set; }
        public int? AppliedByID { get; set; }

        // ── Auditoría ───────────────────────────────────────────────────────

        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Control de concurrencia optimista.</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        // ── Navegación ──────────────────────────────────────────────────────

        public ICollection<ScheduleChangePlanDetail> Details { get; set; } = [];
    }
}
