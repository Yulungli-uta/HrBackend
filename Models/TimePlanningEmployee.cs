namespace WsUtaSystem.Models
{
    public class TimePlanningEmployee
    {
        public int PlanEmployeeID { get; set; }
        public int PlanID { get; set; }
        public int EmployeeID { get; set; }
        public decimal? AssignedHours { get; set; }
        public int? AssignedMinutes { get; set; }
        public decimal? ActualHours { get; set; } = 0;
        public int? ActualMinutes { get; set; } = 0;
        public int EmployeeStatusTypeID { get; set; }
        public decimal? PaymentAmount { get; set; } = 0;
        public bool IsEligible { get; set; } = true;
        public string? EligibilityReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        //public string? EmployeeName { get; set; }
        //public string? EmployeeStatusName { get; set; }
        //public string? Department { get; set; }
        public TimePlanning? TimePlanning { get; set; }
    }
}
