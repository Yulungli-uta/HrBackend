using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Reports;

/// <summary>
/// Servicio principal de reportes
/// </summary>
public interface IReportService
{
    // Reportes de Empleados
    Task<byte[]> GenerateEmployeesPdfAsync(ReportFilterDto filter, HttpContext context);
    Task<byte[]> GenerateEmployeesExcelAsync(ReportFilterDto filter, HttpContext context);
    
    // Reportes de Asistencia
    Task<byte[]> GenerateAttendancePdfAsync(ReportFilterDto filter, HttpContext context);
    Task<byte[]> GenerateAttendanceExcelAsync(ReportFilterDto filter, HttpContext context);
    
    // Reportes de Departamentos
    Task<byte[]> GenerateDepartmentsPdfAsync(ReportFilterDto filter, HttpContext context);
    Task<byte[]> GenerateDepartmentsExcelAsync(ReportFilterDto filter, HttpContext context);

    // Reportes de Resumen de Asistencia
    Task<byte[]> GenerateAttendancesumaryPdfAsync(ReportFilterDto filter, HttpContext context);
    Task<byte[]> GenerateAttendancesumaryExcelAsync(ReportFilterDto filter, HttpContext context);
}
