namespace WsUtaSystem.Application.DTOs.Employees;
public class EmployeesDto
{
    public int EmployeeId { get; set; }
    public int PersonID { get; set; }
    public int EmployeeType { get; set; }
    public int? DepartmentId { get; set; }
    public int? ImmediateBossId { get; set; }
    public int? JobId { get; set; }
    public int? TipoDocenteLoesTypeId { get; set; }
    public int? CategoriaDocenteLoesTypeId { get; set; }
    public int? BudgetUnitTypeId { get; set; }
    public DateOnly HireDate { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
