using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class VwEmployeeScheduleAtDateRepository : IVwEmployeeScheduleAtDateRepository
{
    private readonly WsUtaSystem.Data.AppDbContext _db;
    public VwEmployeeScheduleAtDateRepository(WsUtaSystem.Data.AppDbContext db) { _db = db; }

    public async Task<List<VwEmployeeScheduleAtDate>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Set<VwEmployeeScheduleAtDate>().ToListAsync(ct);
    }

    public async Task<List<VwEmployeeScheduleAtDate>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _db.Set<VwEmployeeScheduleAtDate>()
            .Where(x => x.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    public async Task<List<VwEmployeeScheduleAtDate>> GetByDateAsync(DateTime date, CancellationToken ct = default)
    {
        return await _db.Set<VwEmployeeScheduleAtDate>()
            .Where(x => x.D.Date == date.Date)
            .ToListAsync(ct);
    }
}

