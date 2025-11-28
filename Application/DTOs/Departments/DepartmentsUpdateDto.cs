namespace WsUtaSystem.Application.DTOs.Departments;
public class DepartmentsUpdateDto
{
    ////public class Departments { get; set; }
    public int DepartmentId { get; set; }
    public int? ParentId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ShortName { get; set; }
    public int DepartmentType { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public int? DeanDirector { get; set; }
    public string? BudgetCode { get; set; }
    public int? Dlevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}
