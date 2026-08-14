namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte de subsidio de cargas familiares. Una fila por
/// empleado: cantidad de cargas familiares aprobadas que califican (menores de la edad
/// tope parametrizada en <c>HR.tbl_Parameters</c> [<c>FAMILY_SUBSIDY_MAX_AGE</c>], o de
/// cualquier edad si tienen discapacidad registrada) multiplicada por el valor base
/// parametrizado (<c>FAMILY_SUBSIDY_BASE_VALUE</c>).
/// </summary>
public sealed record FamilySubsidyReportDto
{
    public int EmployeeId { get; init; }
    public string IdCard { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }
    public int QualifyingDependents { get; init; }
    public decimal UnitValue { get; init; }
    public decimal TotalValue { get; init; }
}
