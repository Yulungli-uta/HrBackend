using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class VwLeaveWindowsService : IVwLeaveWindowsService
{
    private readonly IVwLeaveWindowsRepository _repo;
    public VwLeaveWindowsService(IVwLeaveWindowsRepository repo) { _repo = repo; }

    public async Task<List<VwLeaveWindows>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repo.GetAllAsync(ct);
    }

    public async Task<List<VwLeaveWindows>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _repo.GetByEmployeeIdAsync(employeeId, ct);
    }

    public async Task<List<VwLeaveWindows>> GetByLeaveTypeAsync(string leaveType, CancellationToken ct = default)
    {
        return await _repo.GetByLeaveTypeAsync(leaveType, ct);
    }
}

