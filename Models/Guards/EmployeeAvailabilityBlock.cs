using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class EmployeeAvailabilityBlock : IAuditable
{
    public int BlockId { get; set; }
    public int EmployeeId { get; set; }
    public int SourceTypeId { get; set; }
    public string? SourceTable { get; set; }
    public string? SourceId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int StatusTypeId { get; set; }
    public string? Reason { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual Employees? Employee { get; set; }
    public virtual RefTypes? SourceType { get; set; }
    public virtual RefTypes? StatusType { get; set; }
}
