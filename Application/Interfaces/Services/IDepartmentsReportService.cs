using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Servicio de negocio para el reporte de dependencias/departamentos.</summary>
public interface IDepartmentsReportService
{
    Task<IReadOnlyList<DepartmentReportDto>> GetDepartmentsDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);
}
