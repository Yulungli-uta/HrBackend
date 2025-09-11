
namespace WsUtaSystem.Models;
public class TimeRecoveryPlans {
  public int RecoveryPlanId { get; set; }
  public int EmployeeId { get; set; }
  public int OwedMinutes { get; set; }
  public DateOnly PlanDate { get; set; }
  public TimeOnly FromTime { get; set; }
  public TimeOnly ToTime { get; set; }
  public string? Reason { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime CreatedAt { get; set; }
}
