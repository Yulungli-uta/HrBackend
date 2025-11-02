namespace WsUtaSystem.Application.DTOs.EmployeeSchedules;
public class EmployeeSchedulesCreateDto
{
    //public class EmployeeSchedules { get; set; }
    public int EmpScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public int ScheduleId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
