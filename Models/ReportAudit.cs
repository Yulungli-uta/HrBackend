namespace WsUtaSystem.Models;

/// <summary>
/// Registro de auditoría de generación de reportes (HR.tbl_ReportAudit).
/// </summary>
public class ReportAudit
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string ReportFormat { get; set; } = string.Empty;
    public string? FiltersApplied { get; set; }
    public DateTime GeneratedAt { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? GenerationTimeMs { get; set; }
    public string? ClientIp { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public string? FileName { get; set; }
}
