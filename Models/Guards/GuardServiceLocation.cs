using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class GuardServiceLocation : IAuditable
{
    public int LocationId { get; set; }
    public int? ParentLocationId { get; set; }
    public int? RootLocationId { get; set; }
    public int LocationTypeId { get; set; }
    public string? LocationCode { get; set; }
    public string LocationName { get; set; } = null!;
    public string? Description { get; set; }
    public string? LocationPath { get; set; }
    public int Level { get; set; }
    public bool RequiresCoverage { get; set; }
    public bool IsAssignable { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual GuardServiceLocation? Parent { get; set; }
    public virtual GuardServiceLocation? Root { get; set; }
    public virtual ICollection<GuardServiceLocation> Children { get; set; } = new List<GuardServiceLocation>();
}
