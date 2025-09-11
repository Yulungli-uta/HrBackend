namespace WsUtaSystem.Application.DTOs.PersonnelMovements;
public class PersonnelMovementsCreateDto
{
    //public class PersonnelMovements { get; set; }
    public int MovementId { get; set; }
    public int EmployeeId { get; set; }
    public int ContractId { get; set; }
    public int JobId { get; set; }
    public int OriginDepartmentId { get; set; }
    public int DestinationDepartmentId { get; set; }
    public DateOnly MovementDate { get; set; }
    public string? MovementType { get; set; }
    public string DocumentLocation { get; set; }
    public string Reason { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
