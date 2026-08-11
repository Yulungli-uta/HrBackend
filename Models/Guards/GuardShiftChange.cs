using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardShiftChange : IAuditable
{
    public int ShiftChangeId { get; set; }
    public int PlanningId { get; set; }
    public int OriginalEmployeeId { get; set; }
    public int? ReplacementEmployeeId { get; set; }
    public int OriginalScheduleId { get; set; }
    public int? NewScheduleId { get; set; }
    public DateOnly? NewWorkDate { get; set; }
    public int? NewLocationId { get; set; }
    public int ChangeTypeId { get; set; }
    public int StatusTypeId { get; set; }
    public bool IsActiveForAttendance { get; set; }
    public string Reason { get; set; } = null!;
    public int? RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual GuardShiftPlanning? Planning { get; set; }
    public virtual Employees? OriginalEmployee { get; set; }
    public virtual Employees? ReplacementEmployee { get; set; }
    public virtual Schedules? OriginalSchedule { get; set; }
    public virtual Schedules? NewSchedule { get; set; }
    public virtual GuardServiceLocation? NewLocation { get; set; }
    public virtual RefTypes? ChangeType { get; set; }
    public virtual RefTypes? StatusType { get; set; }
    public virtual Employees? RequesterEmployee { get; set; }
    public virtual Employees? ApproverEmployee { get; set; }
}
