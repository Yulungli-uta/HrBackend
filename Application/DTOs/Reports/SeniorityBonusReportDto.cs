namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte de subsidio de antigüedad. Una fila por empleado:
/// RMU actual (<see cref="Models.SalaryHistory"/>, fila más reciente) multiplicado por el
/// porcentaje parametrizado en <c>HR.tbl_Parameters</c> (<c>SENIORITY_SUBSIDY_PERCENT</c>)
/// y por los años completos de antigüedad (<c>Employees.SeniorityDate</c>, o
/// <c>HireDate</c> como respaldo si no tiene <c>SeniorityDate</c> cargado).
/// </summary>
public sealed record SeniorityBonusReportDto
{
    public int EmployeeId { get; init; }
    public string IdCard { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? LaborRegimeName { get; init; }
    public decimal Rmu { get; init; }
    public int SeniorityYears { get; init; }
    public decimal UnitValue { get; init; }
    public decimal TotalValue { get; init; }
}
