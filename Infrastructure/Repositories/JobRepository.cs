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
    }
}
