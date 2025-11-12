using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IPunchJustificationsRepository : IRepository<PunchJustifications, int> {

    Task<IEnumerable<PunchJustifications>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
    Task<IEnumerable<PunchJustifications>> GetByBossEmployeeId(int BossEmployeeId, CancellationToken ct);
}
