using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IVwAttendanceDayRepository
{
    Task<List<VwAttendanceDay>> GetAllAsync(CancellationToken ct = default);
    Task<List<VwAttendanceDay>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<List<VwAttendanceDay>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}

