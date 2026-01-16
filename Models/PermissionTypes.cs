
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class PermissionTypes : IAuditable{
  public int TypeId { get; set; }
  public string Name { get; set; } = null!;
  public bool DeductsFromVacation { get; set; } = false;
  public bool RequiresApproval { get; set; } = true;
  public bool? AttachedFile { get; set; }
  public int? MaxDays { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? CreatedAt { get; set; }
  public int? UpdatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
}
