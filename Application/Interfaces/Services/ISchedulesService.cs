using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface ISchedulesService : IService<Schedules, int> 
{
    Task<IEnumerable<Schedules>> GetBySheduleAcive(CancellationToken ct);
}
