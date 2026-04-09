using WsUtaSystem.Application.DTOs.VwEmployeeCurrentSchedule;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IEmployeeCurrentScheduleService
    {

        Task<EmployeeCurrentScheduleDto?> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmployeeCurrentScheduleDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmployeeCurrentScheduleDto>> GetByEmployeeIdsAsync(
        IEnumerable<int> employeeIds,
        CancellationToken cancellationToken = default);
    }
}
