namespace WsUtaSystem.Application.DTOs.Reports;

public record CertificationReportDto
{
    public int CertificationId { get; init; }
    public string CertCode { get; init; } = string.Empty;
    public string? CertNumber { get; init; }
    public string? Budget { get; init; }
    public decimal? RmuHour { get; init; }
    public decimal? RmuCon { get; init; }
    public DateTime? CertBudgetDate { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public int? RequestId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public int? NumberOfPeopleRequested { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? CreatedAt { get; init; }
}
