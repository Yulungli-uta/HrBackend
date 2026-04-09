using WsUtaSystem.Application.Common.Services;
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
}
