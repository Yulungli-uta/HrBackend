using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class VwPunchDayService : IVwPunchDayService
{
    private readonly IVwPunchDayRepository _repo;
    public VwPunchDayService(IVwPunchDayRepository repo) { _repo = repo; }

    public async Task<List<VwPunchDay>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repo.GetAllAsync(ct);
    }

    public async Task<List<VwPunchDay>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _repo.GetByEmployeeIdAsync(employeeId, ct);
    }

    public async Task<List<VwPunchDay>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _repo.GetByDateRangeAsync(fromDate, toDate, ct);
    }
}

