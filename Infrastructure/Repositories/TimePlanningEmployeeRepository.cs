using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
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
            var overlaps = await FindOverlappingPlansAsync(
                employeeId, startDate, endDate, startTime, endTime, excludePlanId, ct);
            return overlaps.Count > 0;
        }

        public async Task<IReadOnlyList<TimePlanning>> FindOverlappingPlansAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludePlanId = null,
            CancellationToken ct = default)
        {
            // Un plan puede "desbordar" al día siguiente (fin 00:00 o menor al inicio), por eso el
            // filtro de fechas se amplía un día a cada lado y el cruce exacto se resuelve en memoria.
            var from = startDate.Date.AddDays(-1);
            var to = endDate.Date.AddDays(1);

            var cancelledPlanStatusIds = _db.RefTypes
                .Where(r => r.Category == "PLAN_STATUS" && r.Name == "Cancelado")
                .Select(r => r.TypeId);
            var cancelledEmployeeStatusIds = _db.RefTypes
                .Where(r => r.Category == "EMPLOYEE_PLAN_STATUS" && r.Name == "Cancelado")
                .Select(r => r.TypeId);

            var candidates = await _db.TimePlanningEmployee
                .AsNoTracking()
                .Where(tpe => tpe.EmployeeID == employeeId
                              && tpe.TimePlanning != null
                              && (excludePlanId == null || tpe.PlanID != excludePlanId.Value)
                              && !cancelledEmployeeStatusIds.Contains(tpe.EmployeeStatusTypeID)
                              && !cancelledPlanStatusIds.Contains(tpe.TimePlanning!.PlanStatusTypeID)
                              && tpe.TimePlanning!.StartDate <= to
                              && tpe.TimePlanning!.EndDate >= from)
                .Select(tpe => tpe.TimePlanning!)
                .ToListAsync(ct);

            return candidates
                .Where(p => TimePlanningOverlap.Overlaps(
                    startDate, endDate, startTime, endTime,
                    p.StartDate, p.EndDate, p.StartTime, p.EndTime))
                .OrderBy(p => p.StartDate)
                .ThenBy(p => p.StartTime)
                .ToList();
        }

        public async Task<bool> TryAcquireOverlapLockAsync(CancellationToken ct = default)
        {
            var result = await _db.Database
                .SqlQueryRaw<int>(
                    "DECLARE @r INT; EXEC @r = sp_getapplock @Resource = N'TimePlanningOverlap', " +
                    "@LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 15000; " +
                    "SELECT @r AS [Value];")
                .ToListAsync(ct);

            return result.Count > 0 && result[0] >= 0;
        }

        public async Task<TimePlanning?> GetPlanHeaderAsync(int planId, CancellationToken ct = default)
        {
            return await _db.TimePlanning.AsNoTracking().FirstOrDefaultAsync(p => p.PlanID == planId, ct);
        }

        public async Task<IReadOnlyList<int>> GetActiveEmployeeIdsByPlanAsync(int planId, CancellationToken ct = default)
        {
            var cancelledEmployeeStatusIds = _db.RefTypes
                .Where(r => r.Category == "EMPLOYEE_PLAN_STATUS" && r.Name == "Cancelado")
                .Select(r => r.TypeId);

            return await _db.TimePlanningEmployee
                .AsNoTracking()
                .Where(tpe => tpe.PlanID == planId && !cancelledEmployeeStatusIds.Contains(tpe.EmployeeStatusTypeID))
                .Select(tpe => tpe.EmployeeID)
                .ToListAsync(ct);
        }

        public async Task<bool> IsPlanCancelledAsync(int planId, CancellationToken ct = default)
        {
            return await _db.TimePlanning
                .AsNoTracking()
                .AnyAsync(p => p.PlanID == planId
                    && _db.RefTypes.Any(r => r.TypeId == p.PlanStatusTypeID
                                             && r.Category == "PLAN_STATUS" && r.Name == "Cancelado"), ct);
        }

        public async Task<string?> GetEmployeeDisplayNameAsync(int employeeId, CancellationToken ct = default)
        {
            var person = await _db.Employees
                .AsNoTracking()
                .Where(e => e.EmployeeId == employeeId)
                .Select(e => e.People)
                .FirstOrDefaultAsync(ct);

            return person is null ? null : $"{person.LastName} {person.FirstName}".Trim();
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