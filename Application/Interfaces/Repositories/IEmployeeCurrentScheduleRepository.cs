using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IEmployeeCurrentScheduleRepository
    {
        Task<VwEmployeeCurrentSchedule?> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<VwEmployeeCurrentSchedule>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<VwEmployeeCurrentSchedule>> GetByEmployeeIdsAsync(
        IEnumerable<int> employeeIds,
        CancellationToken cancellationToken = default);
    }
}
