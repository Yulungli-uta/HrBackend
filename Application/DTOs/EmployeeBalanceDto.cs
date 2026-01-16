namespace WsUtaSystem.Application.DTOs
{
    public class EmployeeBalanceDto
    {
        public int EmployeeID { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public DateTime HireDate { get; set; }

        // Vacaciones
        public int VacationMinutes { get; set; }
        public decimal VacationDays { get; set; }

        // Recuperación
        public int RecoveryMinutes { get; set; }
        public decimal RecoveryHours { get; set; }

        // Metadatos
        public DateTime? LastUpdated { get; set; }
    }
}
