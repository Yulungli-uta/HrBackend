using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardShiftCoverageRequirement : IAuditable
{
    public int RequirementId { get; set; }
    public int LocationId { get; set; }
    public int ScheduleId { get; set; }
    public byte DayOfWeek { get; set; }
    public int RequiredGuards { get; set; } = 1;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual GuardServiceLocation? Location { get; set; }
    public virtual Schedules? Schedule { get; set; }
}
