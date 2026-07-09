namespace WsUtaSystem.Models;

/// <summary>Historial de estados de una solicitud de certificado laboral.</summary>
public sealed class EmployeeCertificateStatusHistory
{
    public int HistoryId { get; set; }
    public int RequestId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Observation { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    public EmployeeCertificateRequest? Request { get; set; }
}
