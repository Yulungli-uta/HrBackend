using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardRotationGroupEmployee : IAuditable
{
    public int GroupEmployeeId { get; set; }
    public int GroupId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual GuardRotationGroup? Group { get; set; }
    public virtual Employees? Employee { get; set; }
}
