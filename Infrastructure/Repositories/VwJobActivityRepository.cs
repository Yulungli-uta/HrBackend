using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Repositories;

public class VwJobActivityRepository : IVwJobActivityRepository
{
    private readonly AppDbContext _db;

    public VwJobActivityRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<VwJobActivity>> GetAllAsync(CancellationToken ct = default) =>
        await _db.VwJobActivity.AsNoTracking().ToListAsync(ct);

    public async Task<IEnumerable<VwJobActivity>> GetByJobAsync(int jobId, CancellationToken ct = default) =>
        await _db.VwJobActivity.AsNoTracking()
            .Where(a => a.JobID == jobId)
            .ToListAsync(ct);

    public async Task<IEnumerable<VwJobActivity>> GetActiveAssignmentsAsync(CancellationToken ct = default) =>
        await _db.VwJobActivity.AsNoTracking()
            .Where(a => a.ActivityAssignmentActive)
            .ToListAsync(ct);

    public async Task<IEnumerable<VwJobActivity>> GetActiveByJobAsync(int jobId, CancellationToken ct = default) =>
        await _db.VwJobActivity.AsNoTracking()
            .Where(a => a.JobID == jobId && a.ActivityAssignmentActive)
            .ToListAsync(ct);
}
