using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Horario especial individual (sustituto/maternidad/lactancia/otro caso
/// extraordinario) para empleados de horario normal (no guardias, que ya
/// tienen su propio HR.GuardEmployeeSpecialRule). No lleva ValidFrom/ValidTo
/// propio — la vigencia la controla la fila de HR.tbl_EmployeeSchedules que
/// referencia a esta.
/// </summary>
public class EmployeeSpecialSchedule : IAuditable
{
    public int EmployeeSpecialScheduleId { get; set; }
    /// <summary>2026-09-10: relación directa al empleado — antes solo existía
    /// de forma indirecta vía HR.tbl_EmployeeSchedules.EmployeeSpecialScheduleId,
    /// lo que impedía consultar "de quién es este horario especial" sin JOIN.</summary>
    public int EmployeeId { get; set; }
    public TimeOnly EntryTime { get; set; }
    public TimeOnly ExitTime { get; set; }
    public bool HasLunchBreak { get; set; }
    public TimeOnly? LunchStart { get; set; }
    public TimeOnly? LunchEnd { get; set; }
    public int CaseTypeId { get; set; }
    public string? Reason { get; set; }
    public string? DocumentReference { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual RefTypes? CaseType { get; set; }
    public virtual Employees? Employee { get; set; }
}
