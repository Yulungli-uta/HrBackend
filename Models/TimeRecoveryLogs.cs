
namespace WsUtaSystem.Models;
public class TimeRecoveryLogs {
  public int RecoveryLogId { get; set; }
  public int RecoveryPlanId { get; set; }
  public DateOnly ExecutedDate { get; set; }
  public int MinutesRecovered { get; set; }
  public int? ApprovedBy { get; set; }
  public DateTime? ApprovedAt { get; set; }
}
