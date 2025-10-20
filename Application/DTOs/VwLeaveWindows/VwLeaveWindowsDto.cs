namespace WsUtaSystem.Application.DTOs.VwLeaveWindows
{
    public class VwLeaveWindowsDto
    {
        public int EmployeeId { get; set; }
        public DateTime FromDt { get; set; }
        public DateTime ToDt { get; set; }
        public string LeaveType { get; set; } = null!;
    }
}

