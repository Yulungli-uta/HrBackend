namespace WsUtaSystem.Application.DTOs.Reports
{
    public class AttendanceSumaryDto
    {
        public int EmployeeID { get; set; }
        public string IDCard { get; set; }
        public string NombreCompleto { get; set; }
        public int EmployeeType { get; set; }
        public string ContractType { get; set; }
        public DateTime WorkDate { get; set; }
        public int TotalWorkedMinutes { get; set; }
        public int RegularMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
        public int NightMinutes { get; set; }
        public int MinFeriado { get; set; }
        public int MinTotLaboral { get; set; }
        public int Atrazos { get; set; }
        public decimal Alimentacion { get; set; }
        public int MinJustificacion { get; set; }
    }
}
