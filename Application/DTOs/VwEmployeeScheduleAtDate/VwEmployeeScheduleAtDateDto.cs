namespace WsUtaSystem.Application.DTOs.VwEmployeeScheduleAtDate
{
    public class VwEmployeeScheduleAtDateDto
    {
        public int EmployeeId { get; set; }
        public DateTime D { get; set; }
        public int ScheduleId { get; set; }
        public string? ScheduleName { get; set; }
        public TimeSpan? EntryTime { get; set; }
        public TimeSpan? ExitTime { get; set; }
        public decimal RequiredHoursPerDay { get; set; }
        public bool HasLunchBreak { get; set; }
        public TimeSpan? LunchStart { get; set; }
        public TimeSpan? LunchEnd { get; set; }
    }
}

