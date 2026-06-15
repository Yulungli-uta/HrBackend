using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class AcademicLadderRepository : ServiceAwareEfRepository<AcademicLadder, int>, IAcademicLadderRepository
{
    public AcademicLadderRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<List<AcademicLadder>> GetAllOrderedAsync(CancellationToken ct) =>
        await _db.AcademicLadders
            .Include(a => a.CategoryType)
            .Include(a => a.LevelType)
            .Include(a => a.DedicationType)
            .Include(a => a.NextLadder)
            .OrderBy(a => a.Sequence)
            .ToListAsync(ct);

    public async Task<AcademicLadder?> GetNextAsync(int ladderId, CancellationToken ct)
    {
        var current = await _db.AcademicLadders
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.LadderId == ladderId, ct);

        if (current?.NextLadderId is null) return null;

        return await _db.AcademicLadders
            .Include(a => a.CategoryType)
            .Include(a => a.LevelType)
            .FirstOrDefaultAsync(a => a.LadderId == current.NextLadderId, ct);
    }
}
