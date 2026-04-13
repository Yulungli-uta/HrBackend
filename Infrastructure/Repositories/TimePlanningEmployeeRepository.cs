using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.TimePlanningEmployee;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class TimePlanningEmployeeRepository : ServiceAwareEfRepository<TimePlanningEmployee, int>, ITimePlanningEmployeeRepository
    {
        private readonly AppDbContext _db;

        public TimePlanningEmployeeRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TimePlanningEmployee>> GetByPlanIdAsync(int planId, CancellationToken ct = default)
        {
            return await _db.TimePlanningEmployee
                .AsNoTracking()
                .Include(tpe => tpe.Employees)
                .Where(tpe => tpe.PlanID == planId)
                .OrderBy(tpe => tpe.EmployeeID)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TimePlanningEmployee>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
        {
            return await _db.TimePlanningEmployee
                .AsNoTracking()
                .Include(tpe => tpe.TimePlanning)
                .Include(tpe => tpe.Employees)
                .Where(tpe => tpe.EmployeeID == employeeId)
                .OrderByDescending(tpe => tpe.TimePlanning!.StartDate)
                .ThenByDescending(tpe => tpe.TimePlanning!.StartTime)
                .ToListAsync(ct);
        }

        public async Task<TimePlanningEmployee?> GetByPlanAndEmployeeAsync(int planId, int employeeId, CancellationToken ct = default)
        {
            return await _db.TimePlanningEmployee
                .AsNoTracking()
                .Include(tpe => tpe.TimePlanning)
                .Include(tpe => tpe.Employees)
                .FirstOrDefaultAsync(tpe => tpe.PlanID == planId && tpe.EmployeeID == employeeId, ct);
        }

        public async Task<IEnumerable<TimePlanningEmployee>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            return await _db.TimePlanningEmployee
                .AsNoTracking()
                .Include(tpe => tpe.TimePlanning)
                .Include(tpe => tpe.Employees)
                .Where(tpe => tpe.EmployeeStatusTypeID == statusTypeId)
                .OrderByDescending(tpe => tpe.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<int> GetEmployeeCountByPlanAsync(int planId, CancellationToken ct = default)
        {
            return await _db.TimePlanningEmployee
                .AsNoTracking()
                .CountAsync(tpe => tpe.PlanID == planId, ct);
        }

        public async Task<bool> ExistsOverlapAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludePlanId = null,
            CancellationToken ct = default)
        {
            return await _db.TimePlanningEmployee
                .AsNoTracking()
                .Include(tpe => tpe.TimePlanning)
                .AnyAsync(tpe =>
                    tpe.EmployeeID == employeeId &&
                    tpe.TimePlanning != null &&
                    (excludePlanId == null || tpe.PlanID != excludePlanId.Value) &&
                    tpe.TimePlanning.StartDate.Date <= endDate.Date &&
                    tpe.TimePlanning.EndDate.Date >= startDate.Date &&
                    tpe.TimePlanning.StartTime < endTime &&
                    tpe.TimePlanning.EndTime > startTime,
                    ct);
        }

        public async Task<bool> UpdateEmployeeStatusAsync(int planEmployeeId, int newStatusTypeId, CancellationToken ct = default)
        {
            var planningEmployee = await _db.TimePlanningEmployee
                .FirstOrDefaultAsync(x => x.PlanEmployeeID == planEmployeeId, ct);

            if (planningEmployee == null)
                return false;

            planningEmployee.EmployeeStatusTypeID = newStatusTypeId;

            return await _db.SaveChangesAsync(ct) > 0;
        }

        public async Task<bool> BulkUpdateStatusAsync(int planId, int newStatusTypeId, CancellationToken ct = default)
        {
            var employees = await _db.TimePlanningEmployee
                .Where(tpe => tpe.PlanID == planId)
                .ToListAsync(ct);

            if (!employees.Any())
                return false;

            foreach (var employee in employees)
            {
                employee.EmployeeStatusTypeID = newStatusTypeId;
            }

            return await _db.SaveChangesAsync(ct) > 0;
        }
    }
}