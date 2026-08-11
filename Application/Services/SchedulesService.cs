using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class SchedulesService : Service<Schedules, int>, ISchedulesService
{
    private readonly ISchedulesRepository _schedulesRepo;

    public SchedulesService(ISchedulesRepository repo) : base(repo)
    {
        _schedulesRepo = repo;
    }

    public Task<IEnumerable<Schedules>> GetBySheduleAcive(CancellationToken ct)
    {
        return _schedulesRepo.GetBySheduleAcive(ct);
    }
}
