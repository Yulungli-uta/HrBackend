
namespace WsUtaSystem.Models;

/// <summary>
/// El CRUD de escritura de esta tabla (TimeRecoveryLogsController/Service/Repository) está
/// sin uso real y marcado [Obsolete] — pero la tabla en sí NO está huérfana: se lee en vivo
/// desde HR.sp_ProcessAttendanceRecoveryDay (etapa 4 del pipeline diario de asistencia) para
/// perdonar la marca de ausencia del día, algo que HR.tbl_TimePlanningExecution no hace. No
/// borrar ni tratar este modelo como código muerto.
/// </summary>
public class TimeRecoveryLogs {
  public int RecoveryLogId { get; set; }
  public int RecoveryPlanId { get; set; }
  public DateOnly ExecutedDate { get; set; }
  public int MinutesRecovered { get; set; }
  public int? ApprovedBy { get; set; }
  public DateTime? ApprovedAt { get; set; }
}
