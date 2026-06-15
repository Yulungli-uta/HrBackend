namespace WsUtaSystem.Application.DTOs.Reports;

public record PersonnelActionReportDto
{
    public int ActionId { get; init; }
    public string? ActionNumber { get; init; }
    public string PersonIdCard { get; init; } = string.Empty;
    public string PersonFullName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }
    public string ActionTypeName { get; init; } = string.Empty;
    public string? ActionCategory { get; init; }
    public DateTime ActionDate { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? InstitutionalProcessName { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public bool HasDocument { get; init; }
}
