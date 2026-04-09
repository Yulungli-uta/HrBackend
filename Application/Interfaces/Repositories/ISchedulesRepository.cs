using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface ISchedulesRepository : IRepository<Schedules, int> 
{
    Task<IEnumerable<Schedules>> GetBySheduleAcive(CancellationToken ct);
}
