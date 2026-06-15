namespace WsUtaSystem.Application.DTOs.Reports;

public record ContractRequestReportDto
{
    public int RequestId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public string? WorkModalityName { get; init; }
    public decimal NumberHour { get; init; }
    public int NumberOfPeopleToHire { get; init; }
    public int TotalPeopleHired { get; init; }
    public int PendingCount { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string? Observation { get; init; }
    public DateTime CreatedAt { get; init; }
}
