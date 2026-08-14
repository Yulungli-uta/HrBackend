using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Reports;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IEmployeesRepository : IRepository<Employees, int> {
    Task<IEnumerable<Employees>> GetSubordinatesByBossIdAsync(int bossId, CancellationToken ct = default);
    Task<IEnumerable<Employees>> GetByPersonIdAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Datos consolidados para el reporte de empleados (régimen, departamento, cargo, sueldo actual)
    /// resueltos vía EF Core — sin depender de stored procedures.
    /// Sueldo: último HR.tbl_SalaryHistory del contrato más reciente (prioriza Status=VIGENTE) por persona.
    /// Limitación conocida: no cubre sueldo por nombramiento/acción de personal sin contrato asociado.
    /// </summary>
    Task<IEnumerable<EmployeeReportDto>> GetEmployeesReportDataAsync(
        int? departmentId,
        int? employeeType,
        bool? isActive,
        DateTime? hireDateFrom,
        DateTime? hireDateTo,
        int? laborRegimeId = null,
        CancellationToken ct = default);
}
