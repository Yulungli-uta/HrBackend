using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardVacationRequest : IAuditable
{
    public int GuardVacationRequestId { get; set; }
    public int EmployeeId { get; set; }
    public int? GuardVacationPlanId { get; set; }
    public int? VacationId { get; set; }
    public int RequestTypeId { get; set; }
    public DateOnly OriginalStartDate { get; set; }
    public DateOnly OriginalEndDate { get; set; }
    public DateOnly? RequestedStartDate { get; set; }
    public DateOnly? RequestedEndDate { get; set; }
    public int SourceYear { get; set; }
    public int? TargetYear { get; set; }
    public string Reason { get; set; } = null!;
    public int StatusTypeId { get; set; }
    public int? RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public int? DirectionApprovedBy { get; set; }
    public DateTime? DirectionApprovedAt { get; set; }
    public int? SubmittedToDirectionBy { get; set; }
    public DateTime? SubmittedToDirectionAt { get; set; }
    public int? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual Employees? Employee { get; set; }
    public virtual GuardVacationPlan? Plan { get; set; }
    public virtual RefTypes? RequestType { get; set; }
    public virtual RefTypes? StatusType { get; set; }
    public virtual Employees? Requester { get; set; }
    public virtual Employees? DirectionApprover { get; set; }
    public virtual Employees? SubmittedByEmployee { get; set; }
    public virtual Employees? Rejector { get; set; }
}
