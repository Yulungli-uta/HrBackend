
namespace WsUtaSystem.Models;
public class RefTypes {
  public int TypeId { get; set; }
  public string Category { get; set; } = null!;
  public string Name { get; set; } = null!;
  public string? Description { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; }
}
