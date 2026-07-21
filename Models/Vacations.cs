
using WsUtaSystem.Application.Interfaces.Auditable;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Models;
public class Vacations : IAuditable, ISoftDeletable{
  public int VacationId { get; set; }
  public bool IsDeleted { get; set; } = false;
  public int EmployeeId { get; set; }
  public DateOnly StartDate { get; set; }
  public DateOnly EndDate { get; set; }
  public int DaysGranted { get; set; }
  public int DaysTaken { get; set; }
  public int? ApprovedBy { get; set; }
  public DateTime? ApprovedAt { get; set; }
  public string Status { get; set; } = null!;
  public int? CreatedBy { get; set; }
  public DateTime? CreatedAt { get; set; }
  public int? UpdatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public virtual Employees Employee { get; set; }
}
