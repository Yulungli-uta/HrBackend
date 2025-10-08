using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface ITimePlanningExecutionRepository : IRepository<TimePlanningExecution, int>
    {
        Task<IEnumerable<TimePlanningExecution>> GetByPlanEmployeeIdAsync(int planEmployeeId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningExecution>> GetByEmployeeAndDateAsync(int employeeId, DateTime workDate, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningExecution>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningExecution>> GetByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
        Task<TimePlanningExecution> GetLatestExecutionAsync(int planEmployeeId, CancellationToken ct = default);
        Task<bool> VerifyExecutionAsync(int executionId, int verifiedBy, string? comments = null, CancellationToken ct = default);
    }
}
