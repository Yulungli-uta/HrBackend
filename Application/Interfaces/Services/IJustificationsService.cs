using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IJustificationsService : IService<PunchJustifications, int> 
{
    Task ApplyJustificationsAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);

    Task<IEnumerable<PunchJustifications>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
    Task<IEnumerable<PunchJustifications>> GetByBossEmployeeId(int BossEmployeeId, CancellationToken ct);
}

