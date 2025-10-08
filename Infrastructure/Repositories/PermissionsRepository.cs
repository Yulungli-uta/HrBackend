using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PermissionsRepository : ServiceAwareEfRepository<Permissions, int>, IPermissionsRepository
{
    private readonly DbContext _db;
    public PermissionsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct)
    {
        return await _db.Set<Permissions>()
                .Where(rt => rt.EmployeeId == EmployeeId)
                .ToListAsync(ct);
    }
}
