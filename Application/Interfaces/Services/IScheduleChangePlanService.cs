using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.ScheduleChange;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IScheduleChangePlanService
    {
        Task<ScheduleChangePlanResponse?> GetByIdAsync(int planId, CancellationToken ct = default);
        Task<IEnumerable<ScheduleChangePlanResponse>> GetByBossIdAsync(int bossId, CancellationToken ct = default);
        Task<IEnumerable<ScheduleChangePlanResponse>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default);
        Task<PagedResult<ScheduleChangePlanResponse>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ScheduleChangePlanResponse> CreateAsync(CreateScheduleChangePlanRequest request, CancellationToken ct = default);
        Task ApproveAsync(ApproveScheduleChangePlanRequest request, CancellationToken ct = default);
        Task CancelAsync(CancelScheduleChangePlanRequest request, CancellationToken ct = default);
    }
}
