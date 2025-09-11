using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class SchedulesService : Service<Schedules, int>, ISchedulesService
{
    public SchedulesService(ISchedulesRepository repo) : base(repo) { }
}
