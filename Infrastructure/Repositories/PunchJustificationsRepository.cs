using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PunchJustificationsRepository : ServiceAwareEfRepository<PunchJustifications, int>, IPunchJustificationsRepository
{
    private readonly DbContext _db;
    public PunchJustificationsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<PunchJustifications>> GetByEmployeeId(int EmployeeId, CancellationToken ct)
    {
        return await _db.Set<PunchJustifications>()
                .Where(pj => pj.EmployeeId == EmployeeId)
                .ToListAsync(ct);
    }

    public async Task<IEnumerable<PunchJustifications>> GetByBossEmployeeId(int BossEmployeeId, CancellationToken ct)
    {
        return await _db.Set<PunchJustifications>()
                .Where(pj => pj.BossEmployeeId == BossEmployeeId)
                .ToListAsync(ct);
    }


}
