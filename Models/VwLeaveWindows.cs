namespace WsUtaSystem.Models
{
    public class VwLeaveWindows
    {
        public int EmployeeId { get; set; }
        public DateTime FromDt { get; set; }
        public DateTime ToDt { get; set; }
        public string LeaveType { get; set; } = null!;
    }
}

