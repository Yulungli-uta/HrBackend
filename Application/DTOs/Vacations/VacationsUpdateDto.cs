namespace WsUtaSystem.Application.DTOs.Vacations;
public class VacationsUpdateDto
{
    //public class Vacations { get; set; }
    public int VacationId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DaysGranted { get; set; }
    public int DaysTaken { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
