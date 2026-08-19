using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.MassVacationPlan;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class MassVacationPlanRepository : ServiceAwareEfRepository<MassVacationPlan, int>, IMassVacationPlanRepository
{
    private readonly WsUtaSystem.Data.AppDbContext _db;

    public MassVacationPlanRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<List<MassVacationPlanRosterItemDto>> GetRosterAsync(int planId, CancellationToken ct)
    {
        var plan = await _db.Set<MassVacationPlan>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == planId, ct);
        if (plan is null) return [];

        var excluded = await _db.Set<MassVacationPlanExclusion>().AsNoTracking()
            .Where(x => x.PlanId == planId)
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Reason, ct);

        var query =
            from e in _db.Employees.AsNoTracking()
            where e.IsActive
            join person in _db.People.AsNoTracking() on e.PersonID equals person.PersonId
            join dept in _db.Departments.AsNoTracking() on e.DepartmentId equals dept.DepartmentId into deptJoin
            from dept in deptJoin.DefaultIfEmpty()
            select new { e, person, dept };

        if (plan.DepartmentId.HasValue)
            query = query.Where(x => x.e.DepartmentId == plan.DepartmentId.Value);

        var employees = await query.ToListAsync(ct);

        return employees
            .Select(x => new MassVacationPlanRosterItemDto
            {
                EmployeeId = x.e.EmployeeId,
                IdCard = x.person.IdCard,
                FullName = $"{x.person.LastName} {x.person.FirstName}",
                DepartmentName = x.dept?.Name,
                IsExcluded = excluded.ContainsKey(x.e.EmployeeId),
                ExclusionReason = excluded.TryGetValue(x.e.EmployeeId, out var reason) ? reason : null,
            })
            .OrderBy(x => x.FullName)
            .ToList();
    }

    public Task<MassVacationPlanExclusion?> GetExclusionAsync(int planId, int employeeId, CancellationToken ct) =>
        _db.Set<MassVacationPlanExclusion>().FirstOrDefaultAsync(x => x.PlanId == planId && x.EmployeeId == employeeId, ct);

    public async Task AddExclusionAsync(MassVacationPlanExclusion exclusion, CancellationToken ct)
    {
        _db.Set<MassVacationPlanExclusion>().Add(exclusion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveExclusionAsync(MassVacationPlanExclusion exclusion, CancellationToken ct)
    {
        _db.Set<MassVacationPlanExclusion>().Remove(exclusion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<int>> GetIncludedEmployeeIdsAsync(int planId, CancellationToken ct)
    {
        var plan = await _db.Set<MassVacationPlan>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == planId, ct);
        if (plan is null) return [];

        var excludedIds = await _db.Set<MassVacationPlanExclusion>().AsNoTracking()
            .Where(x => x.PlanId == planId)
            .Select(x => x.EmployeeId)
            .ToListAsync(ct);

        var query = _db.Employees.AsNoTracking().Where(e => e.IsActive);
        if (plan.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == plan.DepartmentId.Value);

        return await query
            .Where(e => !excludedIds.Contains(e.EmployeeId))
            .Select(e => e.EmployeeId)
            .ToListAsync(ct);
    }
}
