namespace WsUtaSystem.Application.DTOs.Employees;
public class EmployeesDto
{
    //public class Employees { get; set; }
    public int EmployeeId { get; set; }
    public int Type { get; set; }
    public int DepartmentId { get; set; }
    public int ImmediateBossId { get; set; }
    public DateOnly HireDate { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}
