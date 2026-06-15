using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Reports;

/// <summary>
/// Repositorio para obtener datos de reportes legacy (v1) via stored procedures.
/// Los reportes v2 acceden a datos a través de IReportSource + servicios de aplicación (EF Core).
/// </summary>
public interface IReportRepository
{
    /// <summary>Obtiene datos de empleados para el generador v1 de empleados.</summary>
    Task<IEnumerable<EmployeeReportDto>> GetEmployeesReportDataAsync(ReportFilterDto filter);

    /// <summary>Obtiene datos de asistencia para el generador v1 de asistencia.</summary>
    Task<IEnumerable<AttendanceReportDto>> GetAttendanceReportDataAsync(ReportFilterDto filter);

    /// <summary>Obtiene datos de dependencias para el generador v1 de dependencias.</summary>
    Task<IEnumerable<DepartmentReportDto>> GetDepartmentsReportDataAsync(ReportFilterDto filter);

    /// <summary>Obtiene resumen de asistencia para AttendanceSummaryReportSource (v2 legacy).</summary>
    Task<IEnumerable<AttendanceSumaryDto>> GetAttendanceSumaryReportDataAsync(ReportFilterDto filter);
}
