using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardVacationPlan : IAuditable
{
    public int GuardVacationPlanId { get; set; }
    public int EmployeeId { get; set; }
    public int VacationYear { get; set; }
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly PlannedEndDate { get; set; }
    public int StatusTypeId { get; set; }
    public int? DirectionApprovedBy { get; set; }
    public DateTime? DirectionApprovedAt { get; set; }
    public int? SubmittedToDirectionBy { get; set; }
    public DateTime? SubmittedToDirectionAt { get; set; }
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual Employees? Employee { get; set; }
    public virtual RefTypes? StatusType { get; set; }
    public virtual Employees? DirectionApprover { get; set; }
    public virtual Employees? SubmittedByEmployee { get; set; }
    public virtual ICollection<GuardVacationRequest> Requests { get; set; } = new List<GuardVacationRequest>();
}
