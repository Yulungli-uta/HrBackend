using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EmployeesService : Service<Employees, int>, IEmployeesService
{
    private readonly IEmployeesRepository _Repo;
    private readonly IEmployeeLaborRegimeRepository _regimeRepo;
    private readonly IAppClock _clock;

    public EmployeesService(IEmployeesRepository repo, IEmployeeLaborRegimeRepository regimeRepo, IAppClock clock) : base(repo)
    {
        _Repo = repo;
        _regimeRepo = regimeRepo;
        _clock = clock;
    }

    /// <summary>
    /// Guarda la ficha y, si el usuario cambió el cargo o la unidad, los copia a la fila de régimen principal vigente
    /// (la lista de posiciones lee primero la fila; sin esta copia el cambio no se veía). Solo cuenta lo que realmente cambió:
    /// guardar la ficha sin tocar el cargo no pisa el cargo que viene del contrato o de la acción.
    /// </summary>
    public override async Task UpdateAsync(int id, Employees entity, CancellationToken ct)
    {
        var before = await _repo.GetByIdAsync(id, ct);
        var previousJobId = before?.JobId;
        var previousDepartmentId = before?.DepartmentId;

        await base.UpdateAsync(id, entity, ct);

        var jobChanged = entity.JobId is not null && entity.JobId != previousJobId;
        var departmentChanged = entity.DepartmentId is not null && entity.DepartmentId != previousDepartmentId;
        if (jobChanged || departmentChanged)
            await _regimeRepo.ApplyPositionToCurrentPrincipalAsync(id, jobChanged ? entity.JobId : null, departmentChanged ? entity.DepartmentId : null, _clock.Today, ct);
    }

    public async Task<IEnumerable<Employees>> GetSubordinatesByBossIdAsync(
        int bossId,
        CancellationToken ct = default)
    {
        return await _Repo.GetSubordinatesByBossIdAsync(bossId, ct);
    }

    public async Task<IEnumerable<Employees>> GetByPersonIdAsync(int personId, bool includeInactive = false, CancellationToken ct = default)
    {
        return await _Repo.GetByPersonIdAsync(personId, includeInactive, ct);
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
