namespace WsUtaSystem.Application.DTOs.Permissions;
public class PermissionsCreateDto
{
    //public class Permissions { get; set; }
    public int PermissionId { get; set; }
    public int EmployeeId { get; set; }
    public int PermissionTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool ChargedToVacation { get; set; }
    public decimal HourTaken { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Justification { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; }
    public int? VacationId { get; set; }
}
