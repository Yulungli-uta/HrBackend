using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IScheduleChangePlanRepository
    {
        Task<ScheduleChangePlan?> GetByIdAsync(int planId, CancellationToken ct = default);
        Task<IEnumerable<ScheduleChangePlan>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<ScheduleChangePlan>> GetByBossIdAsync(int bossId, CancellationToken ct = default);
        Task<IEnumerable<ScheduleChangePlan>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default);
        Task<PagedResult<ScheduleChangePlan>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ScheduleChangePlan> CreateAsync(ScheduleChangePlan plan, CancellationToken ct = default);
        Task UpdateAsync(ScheduleChangePlan plan, CancellationToken ct = default);
    }
}
