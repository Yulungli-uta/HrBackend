using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.TimePlanning;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface ITimePlanningService : IService<TimePlanning, int>
    {
        Task<IEnumerable<TimePlanning>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanning>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanning>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
        //Task<PagedResultDTO<TimePlanning>> SearchAsync(TimePlanningSearchDTO searchDto, CancellationToken ct = default);
        Task<TimePlanning> SubmitForApprovalAsync(int planId, int submittedBy, CancellationToken ct = default);
        Task<TimePlanning> ApprovePlanningAsync(int planId, int approvedBy, int? secondApprover = null, CancellationToken ct = default);
        Task<TimePlanning> RejectPlanningAsync(int planId, int rejectedBy, string reason, CancellationToken ct = default);
        Task<bool> ValidatePlanningAsync(TimePlanningCreateDTO createDto, CancellationToken ct = default);
    }
}
