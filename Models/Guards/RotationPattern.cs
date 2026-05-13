using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Guards;

public class RotationPattern : IAuditable
{
    public int PatternId { get; set; }
    public string? PatternCode { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int PatternTypeId { get; set; }
    public int CycleDays { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual RefTypes? PatternType { get; set; }
    public virtual ICollection<RotationPatternDetail> Details { get; set; } = new List<RotationPatternDetail>();
}
