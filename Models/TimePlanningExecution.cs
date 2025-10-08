namespace WsUtaSystem.Models
{
    public class TimePlanningExecution
    {
        public int ExecutionID { get; set; }
        public int PlanEmployeeID { get; set; }
        public DateTime WorkDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalMinutes { get; set; } = 0;
        public int RegularMinutes { get; set; } = 0;
        public int OvertimeMinutes { get; set; } = 0;
        public int NightMinutes { get; set; } = 0;
        public int HolidayMinutes { get; set; } = 0;
        public int? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        //public string? EmployeeName { get; set; }
        //public string? VerifiedByName { get; set; }
        public TimePlanningEmployee? TimePlanningEmployee { get; set; }
    }
}
