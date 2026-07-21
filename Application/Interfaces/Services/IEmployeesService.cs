using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Reports;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IEmployeesService : IService<Employees, int>
{
    Task<IEnumerable<Employees>> GetSubordinatesByBossIdAsync(int bossId, CancellationToken ct = default);
    Task<IEnumerable<Employees>> GetByPersonIdAsync(int personId, CancellationToken ct = default);

    /// <summary>Datos consolidados para el reporte de empleados (régimen, departamento, cargo, sueldo actual).</summary>
    Task<IEnumerable<EmployeeReportDto>> GetEmployeesReportDataAsync(
        int? departmentId,
        int? employeeType,
        bool? isActive,
        DateTime? hireDateFrom,
        DateTime? hireDateTo,
        CancellationToken ct = default);
}
