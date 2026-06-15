using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardLocationRotationAssignment : IAuditable
{
    public int LocationRotationAssignmentId { get; set; }
    public int LocationRotationPeriodId { get; set; }
    public int? GroupId { get; set; }
    public int? EmployeeId { get; set; }
    public int LocationId { get; set; }
    public int? PriorityTypeId { get; set; }
    public bool IsFixedLocation { get; set; }
    public bool IsFixedSchedule { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual GuardLocationRotationPeriod? Period { get; set; }
    public virtual GuardRotationGroup? Group { get; set; }
    public virtual Employees? Employee { get; set; }
    public virtual GuardServiceLocation? Location { get; set; }
    public virtual RefTypes? PriorityType { get; set; }
}
