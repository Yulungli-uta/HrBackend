using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class TimePlanningRepository : ServiceAwareEfRepository<TimePlanning, int>, ITimePlanningRepository
    {
        private readonly DbContext _db;

        public TimePlanningRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TimePlanning>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            //return await _db.Set<TimePlanning>()
            //    .Include(tp => tp.TimePlanningEmployees)
            //    .Where(tp => tp.TimePlanningEmployees.Any(tpe => tpe.EmployeeID == employeeId))
            //    .OrderByDescending(tp => tp.StartDate)
            //    .ToListAsync(ct);
            return null;
        }

        public async Task<IEnumerable<TimePlanning>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanning>()
                .Where(tp => tp.PlanStatusTypeID == statusTypeId)
                .OrderByDescending(tp => tp.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanning>()
                .Where(tp => tp.StartDate <= endDate && tp.EndDate >= startDate)
                .OrderBy(tp => tp.StartDate)
                .ToListAsync(ct);
        }

        //public async Task<IEnumerable<TimePlanning>> SearchAsync(TimePlanningSearchDTO searchDto, CancellationToken ct = default)
        //{
        //    var query = _db.Set<TimePlanning>().AsQueryable();

        //    if (!string.IsNullOrEmpty(searchDto.PlanType))
        //        query = query.Where(tp => tp.PlanType == searchDto.PlanType);

        //    if (searchDto.PlanStatusTypeID.HasValue)
        //        query = query.Where(tp => tp.PlanStatusTypeID == searchDto.PlanStatusTypeID.Value);

        //    if (searchDto.StartDateFrom.HasValue)
        //        query = query.Where(tp => tp.StartDate >= searchDto.StartDateFrom.Value);

        //    if (searchDto.StartDateTo.HasValue)
        //        query = query.Where(tp => tp.StartDate <= searchDto.StartDateTo.Value);

        //    if (searchDto.CreatedBy.HasValue)
        //        query = query.Where(tp => tp.CreatedBy == searchDto.CreatedBy.Value);

        //    if (!string.IsNullOrEmpty(searchDto.SearchText))
        //        query = query.Where(tp => tp.Title.Contains(searchDto.SearchText) ||
        //                                 (tp.Description != null && tp.Description.Contains(searchDto.SearchText)));

        //    return await query.OrderByDescending(tp => tp.CreatedAt)
        //                    .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
        //                    .Take(searchDto.PageSize)
        //                    .ToListAsync(ct);
        //}

        public async Task<int> GetCountByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanning>()
                .CountAsync(tp => tp.PlanStatusTypeID == statusTypeId, ct);
        }

        public async Task<bool> ChangeStatusAsync(int planId, int newStatusTypeId, int? approvedBy = null, CancellationToken ct = default)
        {
            var planning = await _db.Set<TimePlanning>().FindAsync(new object[] { planId }, ct);
            if (planning == null) return false;

            planning.PlanStatusTypeID = newStatusTypeId;
            if (approvedBy.HasValue)
            {
                planning.ApprovedBy = approvedBy;
                planning.ApprovedAt = DateTime.UtcNow;
            }

            planning.UpdatedAt = DateTime.UtcNow;
            _db.Set<TimePlanning>().Update(planning);

            return await _db.SaveChangesAsync(ct) > 0;
        }
    }
}
