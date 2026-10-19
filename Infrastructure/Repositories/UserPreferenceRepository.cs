using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly AppDbContext _db;

    public UserPreferenceRepository(AppDbContext db) => _db = db;

    public Task<UserPreference?> GetByUserAsync(Guid userId, bool track, CancellationToken ct)
    {
        var query = _db.Set<UserPreference>().AsQueryable();
        if (!track) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.UserId == userId, ct);
    }

    public async Task AddAsync(UserPreference preference, CancellationToken ct) =>
        await _db.Set<UserPreference>().AddAsync(preference, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<bool> DeleteByUserAsync(Guid userId, CancellationToken ct) =>
        await _db.Set<UserPreference>().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct) > 0;
}
