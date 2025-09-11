namespace WsUtaSystem.Application.DTOs.Schedules;
public class SchedulesDto
{
    //public class Schedules { get; set; }
    public int ScheduleId { get; set; }
    public string Description { get; set; }
    public TimeOnly EntryTime { get; set; }
    public TimeOnly ExitTime { get; set; }
    public string WorkingDays { get; set; }
    public decimal RequiredHoursPerDay { get; set; }
    public bool HasLunchBreak { get; set; }
    public TimeOnly LunchStart { get; set; }
    public TimeOnly LunchEnd { get; set; }
    public bool IsRotating { get; set; }
    public string RotationPattern { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
