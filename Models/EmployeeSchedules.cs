
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class EmployeeSchedules : IAuditable{
  public int EmpScheduleId { get; set; }
  public int EmployeeId { get; set; }
  /// <summary>Horario del catálogo compartido. Mutuamente excluyente con EmployeeSpecialScheduleId (CHECK CK_EmployeeSchedules_ScheduleOrSpecial): exactamente uno de los dos va lleno.</summary>
  public int? ScheduleId { get; set; }
  /// <summary>Horario especial individual (sustituto/maternidad/lactancia/otro). Mutuamente excluyente con ScheduleId.</summary>
  public int? EmployeeSpecialScheduleId { get; set; }
  public DateOnly ValidFrom { get; set; }
  public DateOnly? ValidTo { get; set; }
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }

  public virtual EmployeeSpecialSchedule? EmployeeSpecialSchedule { get; set; }
}
