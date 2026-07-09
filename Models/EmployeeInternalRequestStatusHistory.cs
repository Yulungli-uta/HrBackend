namespace WsUtaSystem.Models;

/// <summary>Historial de estados de una solicitud interna del empleado.</summary>
public sealed class EmployeeInternalRequestStatusHistory
{
    public int HistoryId { get; set; }
    public int RequestId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Observation { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    public EmployeeInternalRequest? Request { get; set; }
}
