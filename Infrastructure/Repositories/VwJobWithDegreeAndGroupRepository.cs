using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Repositories;

public class VwJobWithDegreeAndGroupRepository : IVwJobWithDegreeAndGroupRepository
{
    private readonly AppDbContext _db;

    public VwJobWithDegreeAndGroupRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<VwJobWithDegreeAndGroup>> GetAllAsync(CancellationToken ct = default) =>
        await _db.VwJobWithDegreeAndGroup.AsNoTracking().ToListAsync(ct);

    public async Task<IEnumerable<VwJobWithDegreeAndGroup>> GetByGroupAsync(int groupId, CancellationToken ct = default) =>
        await _db.VwJobWithDegreeAndGroup.AsNoTracking()
            .Where(j => j.GroupID == groupId)
            .ToListAsync(ct);

    public async Task<IEnumerable<VwJobWithDegreeAndGroup>> GetWithActiveDegreeAsync(CancellationToken ct = default) =>
        await _db.VwJobWithDegreeAndGroup.AsNoTracking()
            .Where(j => j.DegreeIsActive == true)
            .ToListAsync(ct);

    public async Task<VwJobWithDegreeAndGroup?> GetByIdAsync(int jobId, CancellationToken ct = default) =>
        await _db.VwJobWithDegreeAndGroup.AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobID == jobId, ct);
}
