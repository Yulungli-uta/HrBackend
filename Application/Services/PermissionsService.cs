using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PermissionsService : Service<Permissions, int>, IPermissionsService
{
    private readonly IPermissionsRepository _repository;
    public PermissionsService(IPermissionsRepository repo) : base(repo) {
        _repository=repo;
    }

    public async Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct)
    {

        return await _repository.GetByEmployeeId(EmployeeId, ct);
        //throw new NotImplementedException();
    }

    public async Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct)
    {
        return await _repository.GetByImmediateBossId(immediateBossId, ct);
    }
}
