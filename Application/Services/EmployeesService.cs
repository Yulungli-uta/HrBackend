using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EmployeesService : Service<Employees, int>, IEmployeesService
{
    private readonly IEmployeesRepository _Repo;

    public EmployeesService(IEmployeesRepository repo) : base(repo)
    {
        _Repo = repo;
    }

    public async Task<IEnumerable<Employees>> GetSubordinatesByBossIdAsync(
        int bossId,
        CancellationToken ct = default)
    {
        return await _Repo.GetSubordinatesByBossIdAsync(bossId, ct);
    }

    public async Task<IEnumerable<Employees>> GetByPersonIdAsync(int personId, CancellationToken ct = default)
    {
        return await _Repo.GetByPersonIdAsync(personId, ct);
    }

    public async Task<IEnumerable<EmployeeReportDto>> GetEmployeesReportDataAsync(
        int? departmentId,
        int? employeeType,
        bool? isActive,
        DateTime? hireDateFrom,
        DateTime? hireDateTo,
        int? laborRegimeId = null,
        CancellationToken ct = default)
    {
        return await _Repo.GetEmployeesReportDataAsync(departmentId, employeeType, isActive, hireDateFrom, hireDateTo, laborRegimeId, ct);
    }
}
