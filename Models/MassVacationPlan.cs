using WsUtaSystem.Application.Interfaces.Auditable;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Models;

/// <summary>
/// Planificación masiva de vacaciones (cierre colectivo institucional o por
/// departamento). Un registro = un período completo, no una fila por empleado —
/// las personas que trabajan normalmente durante el período van en
/// <see cref="MassVacationPlanExclusion"/>, no se materializa nada en
/// HR.tbl_Vacations para el resto.
/// </summary>
public class MassVacationPlan : IAuditable, ISoftDeletable
{
    public int PlanId { get; set; }

    /// <summary>NULL = aplica a toda la institución; con valor = solo ese departamento.</summary>
    public int? DepartmentId { get; set; }

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Modo "por horas": con valor, aplica solo esa franja de StartDate
    /// (StartDate debe ser igual a EndDate). NULL = día(s) completo(s).</summary>
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public int VacationYear { get; set; }

    /// <summary>FK a HR.ref_Types, categoría MASS_VACATION_PLAN_STATUS (PLANNED |
    /// IN_PROGRESS | FINISHED | CANCELLED). Las transiciones por fecha son automáticas
    /// (DailyMassVacationPlanTransitionJob); CANCELLED es manual, solo permitido mientras
    /// está en PLANNED.</summary>
    public int StatusTypeId { get; set; }

    public int? ExecutedBy { get; set; }
    public DateTime? ExecutedAt { get; set; }

    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public virtual Departments? Department { get; set; }
    public virtual RefTypes? StatusType { get; set; }
    public virtual ICollection<MassVacationPlanExclusion> Exclusions { get; set; } = [];
}
