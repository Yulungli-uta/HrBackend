using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WsUtaSystem.Models
{
    public class ScheduleChangePlanDetail
    {
        public int DetailID { get; set; }

        // ── Relación con cabecera ────────────────────────────────────────────

        public int PlanID { get; set; }

        // ── Empleado afectado ────────────────────────────────────────────────

        /// <summary>FK → tbl_Employees.</summary>
        public int EmployeeID { get; set; }

        // ── Trazabilidad de horario anterior ─────────────────────────────────

        /// <summary>
        /// FK → tbl_Schedules: horario vigente capturado al crear el detalle.
        /// Permite auditoría y rollback futuro.
        /// </summary>
        public int? PreviousScheduleID { get; set; }

        /// <summary>
        /// FK → tbl_EmployeeSchedules: registro abierto (ValidTo = NULL) 
        /// que será cerrado por el SP de ejecución.
        /// </summary>
        public int? PreviousEmpScheduleID { get; set; }

        // ── Resultado de ejecución ───────────────────────────────────────────

        /// <summary>
        /// FK → tbl_EmployeeSchedules: nuevo registro creado al aplicar el cambio.
        /// Null mientras el plan no ha sido ejecutado.
        /// </summary>
        public int? AppliedEmpScheduleID { get; set; }

        // ── Estado (FK → ref_Types categoria SCH_CHANGE_EMP_STATUS) ─────────

        public int StatusTypeID { get; set; }

        // ── Observaciones ────────────────────────────────────────────────────

        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>Razón cuando el empleado fue marcado como Omitido.</summary>
        [MaxLength(300)]
        public string? OmissionReason { get; set; }

        // ── Ejecución individual ─────────────────────────────────────────────

        public DateTime? AppliedAt { get; set; }

        // ── Auditoría ────────────────────────────────────────────────────────

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Control de concurrencia optimista.</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        // ── Navegación ───────────────────────────────────────────────────────

        [ForeignKey(nameof(PlanID))]
        public ScheduleChangePlan Plan { get; set; } = null!;
    }
}
