using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Repositorio especializado para el reporte de dependencias/departamentos.</summary>
public interface IDepartmentsReportRepository
{
    /// <summary>Obtiene estadísticas de empleados y salario por departamento.</summary>
    Task<IReadOnlyList<DepartmentReportDto>> GetDepartmentsDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);
}
