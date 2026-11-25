using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class JobRepository : ServiceAwareEfRepository<Job, int>, IJobRepository
    {
        public JobRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
        }

        public async Task<IEnumerable<Job>> GetActiveJobsAsync(CancellationToken ct)
        {
            return await _set
                .Where(j => j.IsActive)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Job>> SearchJobsByTitleAsync(string title, CancellationToken ct)
        {
            return await _set
                .Where(j => j.IsActive && EF.Functions.Like(j.Description, $"%{title}%"))
                .OrderBy(j => j.JobID)
                .ToListAsync(ct);
        }

        /// <inheritdoc/>
        public Task<bool> ActiveDescriptionInUseAsync(string description, int? excludeJobId, CancellationToken ct)
        {
            var normalized = description.Trim();
            return _set.AsNoTracking().AnyAsync(j => j.IsActive && j.Description == normalized
                                                     && (excludeJobId == null || j.JobID != excludeJobId), ct);
        }

        /// <inheritdoc/>
        public Task<bool> IsScaleActiveAsync(int groupId, CancellationToken ct) =>
            _db.Set<OccupationalGroup>().AsNoTracking().AnyAsync(g => g.GroupId == groupId && g.IsActive, ct);

        /// <inheritdoc/>
        public Task<string?> GetJobTypeNameAsync(int jobTypeId, CancellationToken ct) =>
            _db.Set<RefTypes>().AsNoTracking().Where(t => t.TypeId == jobTypeId).Select(t => (string?)t.Name).FirstOrDefaultAsync(ct);
    }
}
