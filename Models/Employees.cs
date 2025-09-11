
namespace WsUtaSystem.Models;
public class Employees {
  public int EmployeeId { get; set; } // = PersonId
  public int? Type { get; set; } = null!;
  public int? DepartmentId { get; set; }
  public int? ImmediateBossId { get; set; }
  public DateOnly HireDate { get; set; }
  public bool IsActive { get; set; } = true;
  public int? CreatedBy { get; set; }
  public DateTime CreatedAt { get; set; }
  public int? UpdatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
}
