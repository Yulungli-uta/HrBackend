using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class TimePlanningRepository : ServiceAwareEfRepository<TimePlanning, int>, ITimePlanningRepository
    {
        private readonly AppDbContext _db;

        public TimePlanningRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TimePlanning>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            return await _db.TimePlanning
                .AsNoTracking()
                .Include(tp => tp.Employees)
                .Where(tp => tp.Employees.Any(tpe => tpe.EmployeeID == employeeId))
                .OrderByDescending(tp => tp.StartDate)
                .ThenByDescending(tp => tp.StartTime)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            return await _db.TimePlanning
                .AsNoTracking()
                .Where(tp => tp.PlanStatusTypeID == statusTypeId)
                .OrderByDescending(tp => tp.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByCreateBy(int createBy, CancellationToken ct = default)
        {
            return await _db.TimePlanning
                .AsNoTracking()
                .Where(tp => tp.CreatedBy == createBy)
                .OrderByDescending(tp => tp.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _db.TimePlanning
                .AsNoTracking()
                .Where(tp => tp.StartDate <= endDate && tp.EndDate >= startDate)
                .OrderBy(tp => tp.StartDate)
                .ThenBy(tp => tp.StartTime)
                .ToListAsync(ct);
        }

        public async Task<int> GetCountByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            return await _db.TimePlanning
                .AsNoTracking()
                .CountAsync(tp => tp.PlanStatusTypeID == statusTypeId, ct);
        }

        public async Task<bool> ChangeStatusAsync(int planId, int newStatusTypeId, int? approvedBy = null, CancellationToken ct = default)
        {
            var planning = await _db.TimePlanning.FindAsync(new object[] { planId }, ct);
            if (planning == null)
                return false;

            planning.PlanStatusTypeID = newStatusTypeId;

            if (approvedBy.HasValue)
            {
                planning.ApprovedBy = approvedBy;
                planning.ApprovedAt = DateTime.Now;
            }

            planning.UpdatedAt = DateTime.Now;

            return await _db.SaveChangesAsync(ct) > 0;
        }
    }
}
