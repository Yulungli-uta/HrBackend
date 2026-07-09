namespace WsUtaSystem.Models;

/// <summary>
/// Historial inmutable de cada cambio (asignación, modificación, remoción) sobre
/// <see cref="UserAccessScope"/>. Independiente de la fila "viva", así nunca se
/// pierde el rastro aunque esa fila se reactive o desactive.
/// </summary>
public class UserAccessScopeHistory
{
    public long Id { get; set; }

    public int? ScopeId { get; set; }
    public int EmployeeId { get; set; }
    public int ModuleTypeId { get; set; }

    /// <summary>'Assigned' | 'Modified' | 'Removed'.</summary>
    public string ChangeType { get; set; } = "Assigned";

    public int? PreviousScopeTypeId { get; set; }
    public int? PreviousDepartmentId { get; set; }
    public int? NewScopeTypeId { get; set; }
    public int? NewDepartmentId { get; set; }

    public string ChangedBy { get; set; } = "system";
    public string? ChangeReason { get; set; }
    public DateTime ChangeDateTime { get; set; } = DateTime.Now;
}
