using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardAssignmentValidation : IAuditable
{
    public long ValidationId { get; set; }
    public int EmployeeId { get; set; }
    public int? PlanningId { get; set; }
    public int? ShiftChangeId { get; set; }
    public int ValidationTypeId { get; set; }
    public int ResultTypeId { get; set; }
    public int SeverityTypeId { get; set; }
    public DateTime ValidationDate { get; set; }
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual Employees? Employee { get; set; }
    public virtual GuardShiftPlanning? Planning { get; set; }
    public virtual GuardShiftChange? ShiftChange { get; set; }
    public virtual RefTypes? ValidationType { get; set; }
    public virtual RefTypes? ResultType { get; set; }
    public virtual RefTypes? SeverityType { get; set; }
}
