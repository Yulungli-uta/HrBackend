using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface ITimePlanningRepository : IRepository<TimePlanning, int>
    {
        Task<IEnumerable<TimePlanning>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanning>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanning>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
        //Task<IEnumerable<TimePlanning>> SearchAsync(TimePlanningSearchDTO searchDto, CancellationToken ct = default);
        Task<int> GetCountByStatusAsync(int statusTypeId, CancellationToken ct = default);
        Task<bool> ChangeStatusAsync(int planId, int newStatusTypeId, int? approvedBy = null, CancellationToken ct = default);
    }
}
