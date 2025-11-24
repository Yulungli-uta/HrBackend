namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO para reporte de asistencia
/// </summary>
public record AttendanceReportDto
{
    public DateTime AttendanceDate { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string IdentificationNumber { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string FacultyName { get; init; } = string.Empty;
    public DateTime? CheckIn { get; init; }
    public DateTime? CheckOut { get; init; }
    public decimal? HoursWorked { get; init; }
    public string AttendanceType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
