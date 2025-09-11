
namespace WsUtaSystem.Models;
public class Overtime {
  public int OvertimeId { get; set; }
  public int EmployeeId { get; set; }
  public DateOnly WorkDate { get; set; }
  public string OvertimeType { get; set; } = null!;
  public decimal Hours { get; set; }
  public string Status { get; set; } = null!;
  public int? ApprovedBy { get; set; }
  public int? SecondApprover { get; set; }
  public decimal Factor { get; set; }
  public decimal ActualHours { get; set; }
  public decimal PaymentAmount { get; set; }
  public DateTime CreatedAt { get; set; }
}
