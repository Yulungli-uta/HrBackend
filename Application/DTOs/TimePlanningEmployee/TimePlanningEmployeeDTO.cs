using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.TimePlanningEmployee
{
    public class TimePlanningEmployeeCreateDTO
    {
        //public int PlanEmployeeID { get; set; }
        public int PlanID { get; set; }
        [Required(ErrorMessage = "El ID del empleado es requerido")]
        public int EmployeeID { get; set; }

        // Para Overtime
        [Range(0, 24, ErrorMessage = "Las horas asignadas deben estar entre 0 y 24")]
        public decimal? AssignedHours { get; set; }

        // Para Recovery
        [Range(0, 1440, ErrorMessage = "Los minutos asignados deben estar entre 0 y 1440")]
        public int? AssignedMinutes { get; set; }
    }

    public class TimePlanningEmployeeUpdateDTO
    {
        [Required(ErrorMessage = "El ID de la relación empleado-planificación es requerido")]
        public int PlanEmployeeID { get; set; }

        [Range(0, 24, ErrorMessage = "Las horas reales deben estar entre 0 y 24")]
        public decimal? ActualHours { get; set; }

        [Range(0, 1440, ErrorMessage = "Los minutos reales deben estar entre 0 y 1440")]
        public int? ActualMinutes { get; set; }

        public int? EmployeeStatusTypeID { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El monto de pago debe ser positivo")]
        public decimal? PaymentAmount { get; set; }

        public bool? IsEligible { get; set; }
        public string? EligibilityReason { get; set; }
    }

    public class TimePlanningEmployeeResponseDTO
    {
        public int PlanEmployeeID { get; set; }
        public int PlanID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal? AssignedHours { get; set; }
        public int? AssignedMinutes { get; set; }
        public decimal? ActualHours { get; set; }
        public int? ActualMinutes { get; set; }
        public int EmployeeStatusTypeID { get; set; }
        public string EmployeeStatusName { get; set; } = string.Empty;
        public decimal? PaymentAmount { get; set; }
        public bool IsEligible { get; set; }
        public string? EligibilityReason { get; set; }
        public DateTime CreatedAt { get; set; }

        // Porcentaje de completado
        public decimal CompletionPercentage
        {
            get
            {
                if (AssignedHours > 0) return (ActualHours ?? 0) / AssignedHours.Value * 100;
                if (AssignedMinutes > 0) return (ActualMinutes ?? 0) / (decimal)AssignedMinutes.Value * 100;
                return 0;
            }
        }
    }
}
