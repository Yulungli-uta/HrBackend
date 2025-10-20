namespace WsUtaSystem.Models
{
    public class VwPunchDay
    {
        public int EmployeeId { get; set; }
        public DateTime WorkDate { get; set; }
        public DateTime? FirstIn { get; set; }
        public DateTime? LastOut { get; set; }
    }
}

