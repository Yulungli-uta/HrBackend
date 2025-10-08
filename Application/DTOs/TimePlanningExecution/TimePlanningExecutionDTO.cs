using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.TimePlanningExecution
{
    public class TimePlanningExecutionCreateDTO
    {
        [Required(ErrorMessage = "El ID de la relación empleado-planificación es requerido")]
        public int PlanEmployeeID { get; set; }

        [Required(ErrorMessage = "La fecha de trabajo es requerida")]
        public DateTime WorkDate { get; set; }

        [Required(ErrorMessage = "La hora de inicio es requerida")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "La hora de fin es requerida")]
        public DateTime EndTime { get; set; }

        public string? Comments { get; set; }
        public int? VerifiedBy { get; set; }
    }

    public class TimePlanningExecutionUpdateDTO
    {
        [Required(ErrorMessage = "El ID de ejecución es requerido")]
        public int ExecutionID { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Comments { get; set; }
        public int? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class TimePlanningExecutionResponseDTO
    {
        public int ExecutionID { get; set; }
        public int PlanEmployeeID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalMinutes { get; set; }
        public int RegularMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
        public int NightMinutes { get; set; }
        public int HolidayMinutes { get; set; }
        public int? VerifiedBy { get; set; }
        public string? VerifiedByName { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }

        // Propiedades calculadas
        public TimeSpan TotalTime => TimeSpan.FromMinutes(TotalMinutes);
        public TimeSpan RegularTime => TimeSpan.FromMinutes(RegularMinutes);
        public TimeSpan OvertimeTime => TimeSpan.FromMinutes(OvertimeMinutes);
        public bool IsVerified => VerifiedBy.HasValue;
    }
}
