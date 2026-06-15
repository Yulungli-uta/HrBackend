using WsUtaSystem.Application.Interfaces.Auditable;
using WsUtaSystem.Models;

namespace WsUtaSystem.Models.Guards;

public class GuardRotationGroup : IAuditable
{
    public int GroupId { get; set; }
    public string? GroupCode { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? ParentGroupId { get; set; }
    public int? GroupLevelTypeId { get; set; }
    public string? ColorCode { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual GuardRotationGroup? ParentGroup { get; set; }
    public virtual ICollection<GuardRotationGroup> Subgroups { get; set; } = new List<GuardRotationGroup>();
    public virtual RefTypes? GroupLevelType { get; set; }
    public virtual ICollection<GuardRotationGroupEmployee> Employees { get; set; } = new List<GuardRotationGroupEmployee>();
    public virtual ICollection<GuardGroupRotationPattern> Patterns { get; set; } = new List<GuardGroupRotationPattern>();
}
