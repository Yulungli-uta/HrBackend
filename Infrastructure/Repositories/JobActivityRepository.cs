using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class JobActivityRepository : ServiceAwareEfRepository<JobActivity, int>, IJobActivityRepository
{
    public JobActivityRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<bool> DeleteByKeysAsync(int jobId, int activitiesId, CancellationToken ct)
    {
        // Orden de valores debe coincidir con HasKey(x => new { x.ActivitiesId, x.JobID })
        // en JobActivityConfiguration.
        var entity = await _set.FindAsync(new object?[] { activitiesId, jobId }, ct);
        if (entity is null) return false;

        _set.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
