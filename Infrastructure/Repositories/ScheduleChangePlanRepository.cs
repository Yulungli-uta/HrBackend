using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class ScheduleChangePlanRepository
        : ServiceAwareEfRepository<ScheduleChangePlan, int>, IScheduleChangePlanRepository
    {
        private readonly DbContext _db;

        public ScheduleChangePlanRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
            _db = db;
        }

        private IQueryable<ScheduleChangePlan> Query() =>
            _db.Set<ScheduleChangePlan>()
               .Include(x => x.Details)
               .AsNoTracking();

        public async Task<ScheduleChangePlan?> GetByIdAsync(int planId, CancellationToken ct = default)
        {
            return await Query()
                .FirstOrDefaultAsync(x => x.PlanID == planId, ct);
        }

        public async Task<IEnumerable<ScheduleChangePlan>> GetAllAsync(CancellationToken ct = default)
        {
            return await Query()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ScheduleChangePlan>> GetByBossIdAsync(int bossId, CancellationToken ct = default)
        {
            return await Query()
                .Where(x => x.RequestedByBossID == bossId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ScheduleChangePlan>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            return await Query()
                .Where(x => x.StatusTypeID == statusTypeId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<PagedResult<ScheduleChangePlan>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var query = Query().OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.LongCountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<ScheduleChangePlan>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ScheduleChangePlan> CreateAsync(ScheduleChangePlan plan, CancellationToken ct = default)
        {
            _db.Set<ScheduleChangePlan>().Add(plan);
            await _db.SaveChangesAsync(ct);
            return plan;
        }

        public async Task UpdateAsync(ScheduleChangePlan plan, CancellationToken ct = default)
        {
            _db.Set<ScheduleChangePlan>().Update(plan);
            await _db.SaveChangesAsync(ct);
        }
    }
}
