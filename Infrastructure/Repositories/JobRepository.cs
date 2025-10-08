using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class JobRepository : ServiceAwareEfRepository<Job, int>, IJobRepository
    {
        private readonly DbContext _db;
        public JobRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
        }
        
        public async Task<IEnumerable<Job>> GetActiveJobsAsync(CancellationToken ct)
        {
            return await _db.Set<Job>()
                .Where(predicate: rt => rt.IsActive)
                .ToListAsync(ct); 
        }
        public async Task<IEnumerable<Job>> SearchJobsByTitleAsync(string title, CancellationToken ct)
        {
            return await _db.Set<Job>()
                //.Where(j => j..Title.Contains(title))
                //.OrderByDescending(j => j.CreatedDate)
                .ToListAsync(ct);
        }

    }
}
