using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Models
{
    public class TimePlanning
    {
        public int PlanID { get; set; }
        public string PlanType { get; set; } = string.Empty; // "Overtime", "Recovery"
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? OvertimeType { get; set; }
        public decimal? Factor { get; set; }
        public int? OwedMinutes { get; set; }
        public int PlanStatusTypeID { get; set; }
        public bool RequiresApproval { get; set; } = true;
        public int? ApprovedBy { get; set; }
        public int? SecondApprover { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation properties (opcionales, para cargar datos relacionados)
        //public string? PlanStatusName { get; set; }
        //public string? CreatedByName { get; set; }
        //public string? ApprovedByName { get; set; }
    }
}
