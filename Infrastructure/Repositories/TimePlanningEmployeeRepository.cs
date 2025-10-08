using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class TimePlanningEmployeeRepository : ServiceAwareEfRepository<TimePlanningEmployee, int>, ITimePlanningEmployeeRepository
    {
        private readonly DbContext _db;

        public TimePlanningEmployeeRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TimePlanningEmployee>> GetByPlanIdAsync(int planId, CancellationToken ct = default)
        {
            //return await _db.Set<TimePlanningEmployee>()
            //    .Include(tpe => tpe.Employee)
            //    .Where(tpe => tpe.PlanID == planId)
            //    .OrderBy(tpe => tpe.Employee.LastName)
            //    .ThenBy(tpe => tpe.Employee.FirstName)
            //    .ToListAsync(ct);
            return null;
        }

        public async Task<IEnumerable<TimePlanningEmployee>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanningEmployee>()
                .Include(tpe => tpe.TimePlanning)
                .Where(tpe => tpe.EmployeeID == employeeId)
                .OrderByDescending(tpe => tpe.TimePlanning.StartDate)
                .ToListAsync(ct);
        }

        public async Task<TimePlanningEmployee> GetByPlanAndEmployeeAsync(int planId, int employeeId, CancellationToken ct = default)
        {
            //return await _db.Set<TimePlanningEmployee>()
            //    .Include(tpe => tpe.Employees)
            //    .Include(tpe => tpe.TimePlanning)
            //    .FirstOrDefaultAsync(tpe => tpe.PlanID == planId && tpe.EmployeeID == employeeId, ct);
            return null;
        }

        public async Task<IEnumerable<TimePlanningEmployee>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            //return await _db.Set<TimePlanningEmployee>()
            //    .Include(tpe => tpe.Employees)
            //    .Include(tpe => tpe.TimePlanning)
            //    .Where(tpe => tpe.EmployeeStatusTypeID == statusTypeId)
            //    .ToListAsync(ct);
            return null;
        }

        public async Task<int> GetEmployeeCountByPlanAsync(int planId, CancellationToken ct = default)
        {
            return await _db.Set<TimePlanningEmployee>()
                .CountAsync(tpe => tpe.PlanID == planId, ct);
        }

        public async Task<bool> UpdateEmployeeStatusAsync(int planEmployeeId, int newStatusTypeId, CancellationToken ct = default)
        {
            var planningEmployee = await _db.Set<TimePlanningEmployee>().FindAsync(new object[] { planEmployeeId }, ct);
            if (planningEmployee == null) return false;

            planningEmployee.EmployeeStatusTypeID = newStatusTypeId;
            _db.Set<TimePlanningEmployee>().Update(planningEmployee);

            return await _db.SaveChangesAsync(ct) > 0;
        }

        public async Task<bool> BulkUpdateStatusAsync(int planId, int newStatusTypeId, CancellationToken ct = default)
        {
            var employees = await _db.Set<TimePlanningEmployee>()
                .Where(tpe => tpe.PlanID == planId)
                .ToListAsync(ct);

            foreach (var employee in employees)
            {
                employee.EmployeeStatusTypeID = newStatusTypeId;
            }

            _db.Set<TimePlanningEmployee>().UpdateRange(employees);
            return await _db.SaveChangesAsync(ct) > 0;
        }
    }
}
