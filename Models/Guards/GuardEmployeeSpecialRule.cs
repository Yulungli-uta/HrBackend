using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardEmployeeSpecialRule : IAuditable
{
    public int SpecialRuleId { get; set; }
    public int EmployeeId { get; set; }
    public int? FixedLocationId { get; set; }
    public int? FixedScheduleId { get; set; }
    public bool NoNightShift { get; set; }
    public bool OnlyWeekDays { get; set; }
    public bool WeekendPriority { get; set; }
    public bool NightPriority { get; set; }
    public string? Reason { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual Employees? Employee { get; set; }
    public virtual GuardServiceLocation? FixedLocation { get; set; }
    public virtual Schedules? FixedSchedule { get; set; }
}
