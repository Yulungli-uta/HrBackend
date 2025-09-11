namespace WsUtaSystem.Application.DTOs.Departments;
public class DepartmentsCreateDto
{
    //public class Departments { get; set; }
    public int DepartmentId { get; set; }
    public int FacultyId { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
