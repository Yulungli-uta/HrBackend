using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface ITimePlanningEmployeeService : IService<TimePlanningEmployee, int>
    {
        Task<IEnumerable<TimePlanningEmployee>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningEmployee>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
        Task<TimePlanningEmployee> GetByPlanAndEmployeeAsync(int planId, int employeeId, CancellationToken ct = default);
        Task<TimePlanningEmployee> UpdateEmployeeStatusAsync(int planEmployeeId, int statusTypeId, CancellationToken ct = default);
        Task<bool> BulkUpdateEmployeeStatusAsync(int planId, int statusTypeId, CancellationToken ct = default);
        Task<bool> ValidateEmployeeEligibilityAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    }
}
