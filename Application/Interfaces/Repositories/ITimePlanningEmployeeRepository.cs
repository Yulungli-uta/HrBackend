using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface ITimePlanningEmployeeRepository : IRepository<TimePlanningEmployee, int>
    {
        Task<IEnumerable<TimePlanningEmployee>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningEmployee>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
        Task<TimePlanningEmployee> GetByPlanAndEmployeeAsync(int planId, int employeeId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningEmployee>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default);
        Task<int> GetEmployeeCountByPlanAsync(int planId, CancellationToken ct = default);
        Task<bool> UpdateEmployeeStatusAsync(int planEmployeeId, int newStatusTypeId, CancellationToken ct = default);
        Task<bool> BulkUpdateStatusAsync(int planId, int newStatusTypeId, CancellationToken ct = default);
    }

}
