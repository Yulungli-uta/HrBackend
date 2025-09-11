using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class RefTypesRepository : ServiceAwareEfRepository<RefTypes, int>, IRefTypesRepository
{
    private readonly DbContext _db;
    public RefTypesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<RefTypes>> GetByCategoryAsync(string category, CancellationToken ct)
    {
        return await _db.Set<RefTypes>()
                .Where(rt => rt.Category == category && rt.IsActive)
                .ToListAsync(ct);
    }


}
