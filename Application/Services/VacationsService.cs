using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;

public class VacationsService : Service<Vacations, int>, IVacationsService
{
    private readonly IVacationsRepository _repository;
    public VacationsService(IVacationsRepository repo) : base(repo) { 
        _repository=repo;

    }

    public async Task<IEnumerable<Vacations>> GetByEmployeeId(int EmployeeId, CancellationToken ct)
    {
        return await _repository.GetByEmployeeId(EmployeeId, ct);
        //throw new NotImplementedException();
    }

    public async Task<IEnumerable<Vacations>> GetByImmediateBossId(int immediateBossId, CancellationToken ct)
    {
        return await _repository.GetByImmediateBossId(immediateBossId, ct);
    }
}
