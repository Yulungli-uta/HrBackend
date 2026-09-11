namespace WsUtaSystem.Application.DTOs.EmployeeSchedules;
public class EmployeeSchedulesCreateDto
{
    //public class EmployeeSchedules { get; set; }
    public int EmpScheduleId { get; set; }
    public int EmployeeId { get; set; }
    /// <summary>Horario del catálogo. Nulo si se llena el bloque "Special*" (horario individual).</summary>
    public int? ScheduleId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Horario especial individual (sustituto/maternidad/lactancia/otro) — se
    // usa en vez de ScheduleId, nunca junto con él.
    public TimeOnly? SpecialEntryTime { get; set; }
    public TimeOnly? SpecialExitTime { get; set; }
    public bool? SpecialHasLunchBreak { get; set; }
    public TimeOnly? SpecialLunchStart { get; set; }
    public TimeOnly? SpecialLunchEnd { get; set; }
    public int? SpecialCaseTypeId { get; set; }
    public string? SpecialReason { get; set; }
    public string? SpecialDocumentReference { get; set; }
    public bool SpecialRequiresApproval { get; set; }
}
