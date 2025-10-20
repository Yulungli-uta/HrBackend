namespace WsUtaSystem.Models
{
    public class VwAttendanceDay
    {
        public int EmployeeId { get; set; }
        public DateTime WorkDate { get; set; }
        public decimal? RequiredHoursPerDay { get; set; }
        public TimeSpan? EntryTime { get; set; }
        public TimeSpan? ExitTime { get; set; }
        public bool? HasLunchBreak { get; set; }
        public TimeSpan? LunchStart { get; set; }
        public TimeSpan? LunchEnd { get; set; }
        public DateTime? FirstIn { get; set; }
        public DateTime? LastOut { get; set; }
        public int RequiredMin { get; set; }
    }
}

