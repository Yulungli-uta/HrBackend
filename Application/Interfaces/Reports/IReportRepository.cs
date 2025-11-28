using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Reports;

/// <summary>
/// Repositorio para obtener datos de reportes
/// </summary>
public interface IReportRepository
{
    Task<IEnumerable<EmployeeReportDto>> GetEmployeesReportDataAsync(ReportFilterDto filter);
    Task<IEnumerable<AttendanceReportDto>> GetAttendanceReportDataAsync(ReportFilterDto filter);
    Task<IEnumerable<DepartmentReportDto>> GetDepartmentsReportDataAsync(ReportFilterDto filter);

    Task<IEnumerable<AttendanceSumaryDto>> GetAttendanceSumaryReportDataAsync(ReportFilterDto filter);
}
