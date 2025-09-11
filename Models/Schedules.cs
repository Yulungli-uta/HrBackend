
namespace WsUtaSystem.Models;
public class Schedules {
  public int ScheduleId { get; set; }
  public string Description { get; set; } = null!;
  public TimeOnly EntryTime { get; set; }
  public TimeOnly ExitTime { get; set; }
  public string WorkingDays { get; set; } = null!;
  public decimal RequiredHoursPerDay { get; set; }
  public bool HasLunchBreak { get; set; } = true;
  public TimeOnly? LunchStart { get; set; }
  public TimeOnly? LunchEnd { get; set; }
  public bool IsRotating { get; set; } = false;
  public string? RotationPattern { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? UpdatedAt { get; set; }
}
