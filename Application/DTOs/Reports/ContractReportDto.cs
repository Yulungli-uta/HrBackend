namespace WsUtaSystem.Application.DTOs.Reports;

public record ContractReportDto
{
    public int ContractId { get; init; }
    public string ContractCode { get; init; } = string.Empty;
    public string PersonIdCard { get; init; } = string.Empty;
    public string PersonFullName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string ContractTypeName { get; init; } = string.Empty;
    public string? LaborRegimeName { get; init; }
    public string? WorkModalityName { get; init; }
    public decimal? ContractedHours { get; init; }
    public string? JobTitle { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string StatusName { get; init; } = string.Empty;
}
