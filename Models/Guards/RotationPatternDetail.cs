using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class RotationPatternDetail : IAuditable
{
    public int PatternDetailId { get; set; }
    public int PatternId { get; set; }
    public int DayOrder { get; set; }
    public int? ScheduleId { get; set; }
    public bool IsRestDay { get; set; }
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual RotationPattern? Pattern { get; set; }
    public virtual Schedules? Schedule { get; set; }
}
