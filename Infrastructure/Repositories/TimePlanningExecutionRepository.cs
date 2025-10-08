using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class TimePlanningExecutionRepository : ServiceAwareEfRepository<TimePlanningExecution, int>, ITimePlanningExecutionRepository
    {
        private readonly DbContext _db;

        public TimePlanningExecutionRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TimePlanningExecution>> GetByPlanEmployeeIdAsync(int planEmployeeId, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanningExecution>()
                .Where(tpe => tpe.PlanEmployeeID == planEmployeeId)
                .OrderBy(tpe => tpe.WorkDate)
                .ThenBy(tpe => tpe.StartTime)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TimePlanningExecution>> GetByEmployeeAndDateAsync(int employeeId, DateTime workDate, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanningExecution>()
                .Include(tpe => tpe.TimePlanningEmployee)
                .Where(tpe => tpe.TimePlanningEmployee.EmployeeID == employeeId && tpe.WorkDate == workDate)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TimePlanningExecution>> GetByPlanIdAsync(int planId, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanningExecution>()
                .Include(tpe => tpe.TimePlanningEmployee)
                .Where(tpe => tpe.TimePlanningEmployee.PlanID == planId)
                .OrderBy(tpe => tpe.WorkDate)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TimePlanningExecution>> GetByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanningExecution>()
                .Include(tpe => tpe.TimePlanningEmployee)
                .Where(tpe => tpe.TimePlanningEmployee.EmployeeID == employeeId &&
                             tpe.WorkDate >= startDate && tpe.WorkDate <= endDate)
                .OrderBy(tpe => tpe.WorkDate)
                .ToListAsync(ct);
        }

        public async Task<TimePlanningExecution> GetLatestExecutionAsync(int planEmployeeId, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanningExecution>()
                .Where(tpe => tpe.PlanEmployeeID == planEmployeeId)
                .OrderByDescending(tpe => tpe.WorkDate)
                .ThenByDescending(tpe => tpe.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> VerifyExecutionAsync(int executionId, int verifiedBy, string? comments = null, CancellationToken ct = default)
        {
            var execution = await _db.Set<TimePlanningExecution>().FindAsync(new object[] { executionId }, ct);
            if (execution == null) return false;

            execution.VerifiedBy = verifiedBy;
            execution.VerifiedAt = DateTime.UtcNow;
            execution.Comments = comments;

            _db.Set<TimePlanningExecution>().Update(execution);
            return await _db.SaveChangesAsync(ct) > 0;
        }
    }
}
