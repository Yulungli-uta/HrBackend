using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EmployeesRepository : ServiceAwareEfRepository<Employees, int>, IEmployeesRepository
{
    private readonly WsUtaSystem.Data.AppDbContext _db;

    public EmployeesRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Employees>> GetSubordinatesByBossIdAsync(
        int bossId,
        CancellationToken ct = default)
    {
        return await _db.Set<Employees>()
            .AsNoTracking()
            .Where(e => e.ImmediateBossId == bossId && e.IsActive)
            .OrderBy(e => e.EmployeeId)
            .ToListAsync(ct);
    }
}
