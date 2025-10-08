using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.TimePlanningEmployee;

namespace WsUtaSystem.Application.DTOs.TimePlanning
{
    
    public class TimePlanningCreateDTO
    {
        [Required(ErrorMessage = "El tipo de planificación es requerido")]
        [RegularExpression("^(Overtime|Recovery)$", ErrorMessage = "El tipo debe ser 'Overtime' o 'Recovery'")]
        public string PlanType { get; set; } = string.Empty;

        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "La fecha de fin es requerida")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "La hora de inicio es requerida")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "La hora de fin es requerida")]
        public TimeSpan EndTime { get; set; }

        // Solo para Overtime
        public string? OvertimeType { get; set; }
        public decimal? Factor { get; set; }

        // Solo para Recovery
        public int? OwedMinutes { get; set; }

        public int PlanStatusTypeID { get; set; }

        [Required(ErrorMessage = "El ID del creador es requerido")]
        public int CreatedBy { get; set; }

        public bool RequiresApproval { get; set; } = true;

        // Lista de empleados a asignar
        public List<TimePlanningEmployeeCreateDTO> Employees { get; set; } = new List<TimePlanningEmployeeCreateDTO>();
    }

    public class TimePlanningUpdateDTO
    {
        [Required(ErrorMessage = "El ID de la planificación es requerido")]
        public int PlanID { get; set; }

        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        public string? Title { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? OvertimeType { get; set; }
        public decimal? Factor { get; set; }
        public int? OwedMinutes { get; set; }
        public int? PlanStatusTypeID { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class TimePlanningResponseDTO
    {
        public int PlanID { get; set; }
        public string PlanType { get; set; } = string.Empty;
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
        public string PlanStatusName { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; }
        public int? ApprovedBy { get; set; }
        //public string? ApprovedByName { get; set; }
        public int? SecondApprover { get; set; }
        public string? SecondApproverName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int CreatedBy { get; set; }
        //public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        //public string? UpdatedByName { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<TimePlanningEmployeeResponseDTO> Employees { get; set; } = new List<TimePlanningEmployeeResponseDTO>();
        public int TotalEmployees => Employees.Count;
        public int EligibleEmployees => Employees.Count(e => e.IsEligible);
        public decimal TotalAssignedHours => Employees.Sum(e => e.AssignedHours ?? 0);
        public decimal TotalActualHours => Employees.Sum(e => e.ActualHours ?? 0);
    }
}
