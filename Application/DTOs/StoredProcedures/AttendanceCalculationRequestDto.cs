namespace WsUtaSystem.Application.DTOs.StoredProcedures
{
    public class AttendanceCalculationRequestDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? EmployeeId { get; set; }
    }
}

