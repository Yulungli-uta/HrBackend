using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class VwLeaveWindowsRepository : IVwLeaveWindowsRepository
{
    private readonly WsUtaSystem.Data.AppDbContext _db;
    public VwLeaveWindowsRepository(WsUtaSystem.Data.AppDbContext db) { _db = db; }

    public async Task<List<VwLeaveWindows>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Set<VwLeaveWindows>().ToListAsync(ct);
    }

    public async Task<List<VwLeaveWindows>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _db.Set<VwLeaveWindows>()
            .Where(x => x.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    public async Task<List<VwLeaveWindows>> GetByLeaveTypeAsync(string leaveType, CancellationToken ct = default)
    {
        return await _db.Set<VwLeaveWindows>()
            .Where(x => x.LeaveType == leaveType)
            .ToListAsync(ct);
    }
}

