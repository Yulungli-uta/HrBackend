namespace WsUtaSystem.Application.DTOs.Reports.Common;

/// <summary>
/// Filtros comunes para todos los reportes
/// </summary>
public record ReportFilterDto
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? DepartmentId { get; init; }
    //public int? FacultyId { get; init; }
    public int? EmployeeId { get; init; }
    public string? EmployeeType { get; init; }
    public bool? IsActive { get; init; }
    public bool? IncludeInactive { get; init; }


}
