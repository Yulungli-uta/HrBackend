using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class VwPunchDayRepository : IVwPunchDayRepository
{
    private readonly WsUtaSystem.Data.AppDbContext _db;
    public VwPunchDayRepository(WsUtaSystem.Data.AppDbContext db) { _db = db; }

    public async Task<List<VwPunchDay>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Set<VwPunchDay>().ToListAsync(ct);
    }

    public async Task<List<VwPunchDay>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _db.Set<VwPunchDay>()
            .Where(x => x.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    public async Task<List<VwPunchDay>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _db.Set<VwPunchDay>()
            .Where(x => x.WorkDate >= fromDate && x.WorkDate <= toDate)
            .ToListAsync(ct);
    }
}

