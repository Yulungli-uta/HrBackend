namespace WsUtaSystem.Application.DTOs.Permissions;
public class PermissionsCreateDto
{
    //public class Permissions { get; set; }
    public int PermissionId { get; set; }
    public int EmployeeId { get; set; }
    public int PermissionTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool ChargedToVacation { get; set; }
    public int ApprovedBy { get; set; }
    public string Justification { get; set; }
    public DateTime RequestDate { get; set; }
    public string Status { get; set; }
    public int VacationId { get; set; }
}
