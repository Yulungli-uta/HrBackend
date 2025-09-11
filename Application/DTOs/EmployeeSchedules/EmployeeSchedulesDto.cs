namespace WsUtaSystem.Application.DTOs.EmployeeSchedules;
public class EmployeeSchedulesDto
{
    //public class EmployeeSchedules { get; set; }
    public int EmpScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public int ScheduleId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
}
