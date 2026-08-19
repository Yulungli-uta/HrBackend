namespace WsUtaSystem.Models;

/// <summary>
/// Empleado que trabaja normalmente durante el período de un
/// <see cref="MassVacationPlan"/> — no se le descuenta saldo ni se le exime de
/// marcar asistencia. Solo contiene las excepciones, no el roster completo.
/// </summary>
public class MassVacationPlanExclusion
{
    public int ExclusionId { get; set; }
    public int PlanId { get; set; }
    public int EmployeeId { get; set; }
    public string? Reason { get; set; }

    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual MassVacationPlan? Plan { get; set; }
    public virtual Employees? Employee { get; set; }
}
