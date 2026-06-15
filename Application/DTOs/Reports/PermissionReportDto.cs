namespace WsUtaSystem.Application.DTOs.Reports;

public record PermissionReportDto
{
    public int PermissionId { get; init; }
    public string PersonIdCard { get; init; } = string.Empty;
    public string PersonFullName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string PermissionTypeName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal? HourTaken { get; init; }
    public bool ChargedToVacation { get; init; }
    public string? Justification { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ApprovedByName { get; init; }
}
