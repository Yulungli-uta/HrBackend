
namespace WsUtaSystem.Models;
public class PermissionTypes {
  public int TypeId { get; set; }
  public string Name { get; set; } = null!;
  public bool DeductsFromVacation { get; set; } = false;
  public bool RequiresApproval { get; set; } = true;
  public int? MaxDays { get; set; }
}
