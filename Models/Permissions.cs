
namespace WsUtaSystem.Models;
public class Permissions {
  public int PermissionId { get; set; }
  public int EmployeeId { get; set; }
  public int PermissionTypeId { get; set; }
  public DateOnly StartDate { get; set; }
  public DateOnly EndDate { get; set; }
  public bool ChargedToVacation { get; set; } = false;
  public int? ApprovedBy { get; set; }
  public string? Justification { get; set; }
  public DateTime RequestDate { get; set; }
  public string Status { get; set; } = null!;
  public int? VacationId { get; set; }
}
