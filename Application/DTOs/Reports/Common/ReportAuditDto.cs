namespace WsUtaSystem.Application.DTOs.Reports.Common;

/// <summary>
/// DTO para auditoría de reportes
/// </summary>
public record ReportAuditDto
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty;
    public string ReportFormat { get; init; } = string.Empty;
    public string? FiltersApplied { get; init; }
    public DateTime GeneratedAt { get; init; }
    public long? FileSizeBytes { get; init; }
    public int? GenerationTimeMs { get; init; }
    public string? ClientIp { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? FileName { get; init; }
}

/// <summary>
/// DTO para crear auditoría de reporte
/// </summary>
public record CreateReportAuditDto
{
    public Guid? UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty;
    public string ReportFormat { get; init; } = string.Empty;
    public string? FiltersApplied { get; init; }
    public long? FileSizeBytes { get; init; }
    public int? GenerationTimeMs { get; init; }
    public string? ClientIp { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? FileName { get; init; }
}
