using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IVwAttendanceDayService
{
    Task<List<VwAttendanceDay>> GetAllAsync(CancellationToken ct = default);
    Task<List<VwAttendanceDay>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<List<VwAttendanceDay>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}

