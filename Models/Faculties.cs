
namespace WsUtaSystem.Models;
public class Faculties {
  public int FacultyId { get; set; }
  public string Name { get; set; } = null!;
  public int? DeanEmployeeId { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; }
  public DateTime? UpdatedAt { get; set; }
}
