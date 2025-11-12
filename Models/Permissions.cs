
namespace WsUtaSystem.Models;
public class Permissions {
  public int PermissionId { get; set; }
  public int EmployeeId { get; set; }
  public int PermissionTypeId { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public bool ChargedToVacation { get; set; } = false;
  public decimal? HourTaken { get; set; }
  public int? ApprovedBy { get; set; }
  public DateTime? ApprovedAt { get; set; }
  public string? Justification { get; set; }
  public DateTime CreatedAt { get; set; }
  public string Status { get; set; } = null!;
  public int? VacationId { get; set; }

  public virtual Employees Employee { get; set; }
}
