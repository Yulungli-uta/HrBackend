
namespace WsUtaSystem.Models;
public class EmployeeSchedules {
  public int EmpScheduleId { get; set; }
  public int EmployeeId { get; set; }
  public int ScheduleId { get; set; }
  public DateOnly ValidFrom { get; set; }
  public DateOnly? ValidTo { get; set; }
  public DateTime CreatedAt { get; set; }
}
