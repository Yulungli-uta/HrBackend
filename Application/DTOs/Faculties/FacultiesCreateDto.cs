namespace WsUtaSystem.Application.DTOs.Faculties;
public class FacultiesCreateDto
{
    //public class Faculties { get; set; }
    public int FacultyId { get; set; }
    public string Name { get; set; }
    public int DeanEmployeeId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
