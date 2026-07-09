
namespace WsUtaSystem.Models;
public class Overtime {
  public int OvertimeId { get; set; }
  public int EmployeeId { get; set; }
  public DateOnly WorkDate { get; set; }
  public string OvertimeType { get; set; } = null!;
  public decimal Hours { get; set; }
  public string Status { get; set; } = null!;
  public int? ApprovedBy { get; set; }
  public int? SecondApprover { get; set; }
  public decimal Factor { get; set; }
  public decimal ActualHours { get; set; }
  public decimal PaymentAmount { get; set; }
  public DateTime CreatedAt { get; set; }
  /// <summary>Referencia al plan de horas extra (tbl_TimePlanningEmployees) que originó esta línea, cuando aplica autorización planificada.</summary>
  public int? PlanEmployeeId { get; set; }
  /// <summary>Régimen laboral (ref_Types CONTRACT_TYPE) que originó la línea. Siempre 57=LOSEP para filas generadas por el pipeline de asistencia, único régimen que genera horas extra.</summary>
  public int? LaborRegimeId { get; set; }
}
