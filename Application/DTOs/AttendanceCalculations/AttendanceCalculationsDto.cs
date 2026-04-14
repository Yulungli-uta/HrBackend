namespace WsUtaSystem.Application.DTOs.AttendanceCalculations;

public class AttendanceCalculationsDto
{
    public int CalculationId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }

    public DateTime? FirstPunchIn { get; set; }
    public DateTime? LastPunchOut { get; set; }

    public int TotalWorkedMinutes { get; set; }
    public int RegularMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public int NightMinutes { get; set; }
    public int HolidayMinutes { get; set; }

    public int RequiredMinutes { get; set; }
    public int ScheduledWorkedMin { get; set; }
    public int OffScheduleMin { get; set; }
    public int AbsentMinutes { get; set; }

    public int MinutesLate { get; set; }
    public int TardinessMin { get; set; }
    public int EarlyLeaveMinutes { get; set; }

    public int PermissionMinutes { get; set; }
    public int VacationMinutes { get; set; }
    public int JustificationMinutes { get; set; }
    public int MedicalLeaveMinutes { get; set; }
    public int PaidLeaveMinutes { get; set; }
    public int UnpaidLeaveMinutes { get; set; }
    public int VacationDeductedMinutes { get; set; }
    public int RecoveredMinutes { get; set; }

    public bool JustificationApply { get; set; }
    public bool HasPermission { get; set; }
    public bool HasVacation { get; set; }
    public bool HasJustification { get; set; }
    public bool HasMedicalLeave { get; set; }
    public bool HasManualAdjustment { get; set; }

    public int FoodSubsidy { get; set; }

    public int? AppliedScheduleId { get; set; }
    public TimeOnly? ScheduledEntryTime { get; set; }
    public TimeOnly? ScheduledExitTime { get; set; }
    public TimeOnly? ScheduledLunchStart { get; set; }
    public TimeOnly? ScheduledLunchEnd { get; set; }
    public bool ScheduledHasLunchBreak { get; set; }
    public int ScheduledMinutes { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    public int CalculationVersion { get; set; }
    public string CalculationSource { get; set; } = string.Empty;
}