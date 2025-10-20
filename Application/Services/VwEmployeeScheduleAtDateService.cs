using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class VwEmployeeScheduleAtDateService : IVwEmployeeScheduleAtDateService
{
    private readonly IVwEmployeeScheduleAtDateRepository _repo;
    public VwEmployeeScheduleAtDateService(IVwEmployeeScheduleAtDateRepository repo) { _repo = repo; }

    public async Task<List<VwEmployeeScheduleAtDate>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repo.GetAllAsync(ct);
    }

    public async Task<List<VwEmployeeScheduleAtDate>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _repo.GetByEmployeeIdAsync(employeeId, ct);
    }

    public async Task<List<VwEmployeeScheduleAtDate>> GetByDateAsync(DateTime date, CancellationToken ct = default)
    {
        return await _repo.GetByDateAsync(date, ct);
    }
}

