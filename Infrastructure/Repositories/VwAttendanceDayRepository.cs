using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class VwAttendanceDayRepository : IVwAttendanceDayRepository
{
    private readonly WsUtaSystem.Data.AppDbContext _db;
    public VwAttendanceDayRepository(WsUtaSystem.Data.AppDbContext db) { _db = db; }

    public async Task<List<VwAttendanceDay>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Set<VwAttendanceDay>().ToListAsync(ct);
    }

    public async Task<List<VwAttendanceDay>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _db.Set<VwAttendanceDay>()
            .Where(x => x.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    public async Task<List<VwAttendanceDay>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _db.Set<VwAttendanceDay>()
            .Where(x => x.WorkDate >= fromDate && x.WorkDate <= toDate)
            .ToListAsync(ct);
    }
}

