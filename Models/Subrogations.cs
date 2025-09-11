
namespace WsUtaSystem.Models;
public class Subrogations {
  public int SubrogationId { get; set; }
  public int SubrogatedEmployeeId { get; set; }
  public int SubrogatingEmployeeId { get; set; }
  public DateOnly StartDate { get; set; }
  public DateOnly EndDate { get; set; }
  public int? PermissionId { get; set; }
  public int? VacationId { get; set; }
  public string? Reason { get; set; }
  public DateTime CreatedAt { get; set; }
}
