using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPunchJustificationsService : IService<PunchJustifications, int> {

    Task<IEnumerable<PunchJustifications>> GetByBossEmployeeId(int BossEmployeeId, CancellationToken ct);
}
