using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardRotationGroup : IAuditable
{
    public int GroupId { get; set; }
    public string? GroupCode { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<GuardRotationGroupEmployee> Employees { get; set; } = new List<GuardRotationGroupEmployee>();
    public virtual ICollection<GuardGroupRotationPattern> Patterns { get; set; } = new List<GuardGroupRotationPattern>();
}
