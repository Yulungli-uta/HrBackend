namespace WsUtaSystem.Models
{
    public class VwEmployeeCurrentSchedule
    {
        public int EmployeeId { get; set; }
        public int PersonId { get; set; }
        public int? EmployeeType { get; set; }
        public int? DepartmentId { get; set; }
        public int? ImmediateBossId { get; set; }
        public DateOnly HireDate { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }

        public int EmpScheduleId { get; set; }
        public int ScheduleId { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public DateTime ScheduleAssignedAt { get; set; }
        public int? ScheduleAssignedBy { get; set; }

        public string ScheduleDescription { get; set; } = null!;
        public TimeSpan EntryTime { get; set; }
        public TimeSpan ExitTime { get; set; }
        public string WorkingDays { get; set; } = null!;
        public decimal RequiredHoursPerDay { get; set; }
        public bool HasLunchBreak { get; set; }
        public TimeSpan? LunchStart { get; set; }
        public TimeSpan? LunchEnd { get; set; }
        public bool IsRotating { get; set; }
        public string? RotationPattern { get; set; }
    }
}
