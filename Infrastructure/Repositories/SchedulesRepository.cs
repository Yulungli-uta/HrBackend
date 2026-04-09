using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class SchedulesRepository : ServiceAwareEfRepository<Schedules, int>, ISchedulesRepository
{

    private readonly WsUtaSystem.Data.AppDbContext _db;
    public SchedulesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) 
    {
        _db = db;
    }

    public async Task<IEnumerable<Schedules>> GetBySheduleAcive(CancellationToken ct)
    {
        return await _db.Set<Schedules>()
            .AsNoTracking()
            .Where(e => e.IsRotating)
            .OrderBy(e => e.EntryTime)
            .ToListAsync(ct);
    }

  
}
