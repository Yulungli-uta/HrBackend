
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// El CRUD de escritura de esta tabla (TimeRecoveryPlansController/Service/Repository) está
/// sin uso real y marcado [Obsolete] — pero la tabla en sí NO está huérfana: se lee en vivo
/// desde HR.sp_ProcessAttendanceRecoveryDay (etapa 4 del pipeline diario de asistencia) para
/// perdonar la marca de ausencia del día, algo que HR.tbl_TimePlanning no hace. No borrar ni
/// tratar este modelo como código muerto.
/// </summary>
public class TimeRecoveryPlans : ICreationAuditable{
  public int RecoveryPlanId { get; set; }
  public int EmployeeId { get; set; }
  public int OwedMinutes { get; set; }
  public DateOnly PlanDate { get; set; }
  public TimeOnly FromTime { get; set; }
  public TimeOnly ToTime { get; set; }
  public string? Reason { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? CreatedAt { get; set; }
}
