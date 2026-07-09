
namespace WsUtaSystem.Models;
public class PersonnelMovements {
  public int MovementId { get; set; }
  public int EmployeeId { get; set; }
  public int? ContractId { get; set; }
  public int JobId { get; set; }
  public int? OriginDepartmentId { get; set; }
  public int DestinationDepartmentId { get; set; }
  public DateOnly MovementDate { get; set; }
  /// <summary>FK a HR.ref_Types (Category='MOVEMENT_TYPE'): INGRESO, TRASLADO, ENCARGO, CONTRATO.</summary>
  public int? MovementTypeId { get; set; }
  public string? DocumentLocation { get; set; }
  public string? Reason { get; set; }
  public bool IsActive { get; set; }
  /// <summary>FK a la acción de personal que originó este movimiento.</summary>
  public int? PersonnelActionId { get; set; }

  public int? CreatedBy { get; set; }
  public DateTime CreatedAt { get; set; }

  public virtual RefTypes? MovementType { get; set; }
}
