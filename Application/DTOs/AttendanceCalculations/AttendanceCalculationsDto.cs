namespace WsUtaSystem.Application.DTOs.AttendanceCalculations;
public class AttendanceCalculationsDto
{
    //public class AttendanceCalculations { get; set; }
    public int CalculationId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime FirstPunchIn { get; set; }
    public DateTime LastPunchOut { get; set; }
    public int TotalWorkedMinutes { get; set; }
    public int RegularMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public int NightMinutes { get; set; }
    public int HolidayMinutes { get; set; }
    public string Status { get; set; }
}
