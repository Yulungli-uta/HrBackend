
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class RefTypes : IAuditable{
  public int TypeId { get; set; }
  public string Category { get; set; } = null!;
  public string Name { get; set; } = null!;
  public string? Description { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }

}
