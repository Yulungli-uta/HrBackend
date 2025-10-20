using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class VwAttendanceDayService : IVwAttendanceDayService
{
    private readonly IVwAttendanceDayRepository _repo;
    public VwAttendanceDayService(IVwAttendanceDayRepository repo) { _repo = repo; }

    public async Task<List<VwAttendanceDay>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repo.GetAllAsync(ct);
    }

    public async Task<List<VwAttendanceDay>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _repo.GetByEmployeeIdAsync(employeeId, ct);
    }

    public async Task<List<VwAttendanceDay>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _repo.GetByDateRangeAsync(fromDate, toDate, ct);
    }
}

