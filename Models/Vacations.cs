
namespace WsUtaSystem.Models;
public class Vacations {
  public int VacationId { get; set; }
  public int EmployeeId { get; set; }
  public DateOnly StartDate { get; set; }
  public DateOnly EndDate { get; set; }
  public int DaysGranted { get; set; }
  public int DaysTaken { get; set; }
  public int? ApprovedBy { get; set; }
  public DateTime? ApprovedAt { get; set; }
  public string Status { get; set; } = null!;
  public DateTime CreatedAt { get; set; }
  public DateTime? UpdatedAt { get; set; }
}
