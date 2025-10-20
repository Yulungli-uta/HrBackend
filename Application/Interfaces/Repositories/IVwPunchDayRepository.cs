using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IVwPunchDayRepository
{
    Task<List<VwPunchDay>> GetAllAsync(CancellationToken ct = default);
    Task<List<VwPunchDay>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<List<VwPunchDay>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}

