using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IAttendancePunchesRepository : IRepository<AttendancePunches, int>
{
    Task<IEnumerable<AttendancePunches>> GetLastPunchAsync(int employeeId, CancellationToken ct);
    Task<IEnumerable<AttendancePunches>> GetTodayPunchesByEmployeeAsync(int employeeId, CancellationToken ct);
    Task<IEnumerable<AttendancePunches>> GetPunchesByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct);
    Task<IEnumerable<AttendancePunches>> GetPunchesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct);
}
