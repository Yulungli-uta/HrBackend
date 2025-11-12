using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PunchJustificationsService : Service<PunchJustifications, int>, IPunchJustificationsService
{
    private readonly IPunchJustificationsRepository _repository;
    public PunchJustificationsService(IPunchJustificationsRepository repo) : base(repo) { 
        _repository=repo;
    }

    public async Task<IEnumerable<PunchJustifications>> GetByBossEmployeeId(int BossEmployeeId, CancellationToken ct)
    {
        return await _repository.GetByBossEmployeeId(BossEmployeeId, ct);
    }
}
