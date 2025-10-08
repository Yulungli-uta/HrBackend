using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface ITimePlanningExecutionService : IService<TimePlanningExecution, int>
    {
        Task<IEnumerable<TimePlanningExecution>> GetByPlanEmployeeIdAsync(int planEmployeeId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningExecution>> GetByEmployeeAndDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
        Task<TimePlanningExecution> RegisterWorkTimeAsync(int planEmployeeId, DateTime workDate, DateTime startTime, DateTime endTime, string? comments = null, CancellationToken ct = default);
        Task<TimePlanningExecution> VerifyExecutionAsync(int executionId, int verifiedBy, string? comments = null, CancellationToken ct = default);
        Task<bool> BulkVerifyExecutionsAsync(int planId, int verifiedBy, CancellationToken ct = default);
    }
}
