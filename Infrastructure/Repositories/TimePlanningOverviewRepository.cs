using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TimePlanning;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>Consultas EF de solo lectura sobre planificaciones de tiempo (horas extra / recuperación).</summary>
public sealed class TimePlanningOverviewRepository : ITimePlanningOverviewRepository
{
    private readonly AppDbContext _db;

    public TimePlanningOverviewRepository(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<PagedResult<TimePlanningOverviewItemDto>> GetPlansPagedAsync(
        TimePlanningOverviewFilter filter, CancellationToken ct = default)
    {
        var plans = _db.TimePlanning.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.PlanType))
            plans = plans.Where(p => p.PlanType == filter.PlanType);
        if (filter.StartDate.HasValue)
            plans = plans.Where(p => p.EndDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            plans = plans.Where(p => p.StartDate <= filter.EndDate.Value);
        if (filter.PlanStatusTypeId.HasValue)
            plans = plans.Where(p => p.PlanStatusTypeID == filter.PlanStatusTypeId.Value);

        // Los filtros por empleado (estado, departamento, alcance) se resuelven con EXISTS sobre
        // los empleados del plan para no duplicar planes en el resultado.
        var employeeStatus = filter.EmployeeStatusTypeId;
        var departmentId = filter.DepartmentId;
        var allowed = filter.AllowedDepartmentIds?.ToList();
        if (employeeStatus.HasValue || departmentId.HasValue || allowed is not null)
        {
            plans = plans.Where(p => _db.TimePlanningEmployee.Any(pe =>
                pe.PlanID == p.PlanID
                && (!employeeStatus.HasValue || pe.EmployeeStatusTypeID == employeeStatus.Value)
                && _db.Employees.Any(e => e.EmployeeId == pe.EmployeeID
                    && (!departmentId.HasValue || e.DepartmentId == departmentId.Value)
                    && (allowed == null || (e.DepartmentId != null && allowed.Contains(e.DepartmentId.Value))))));
        }

        // Búsqueda: título del plan o cédula/nombre de alguno de sus empleados.
        var search = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            plans = plans.Where(p => p.Title.Contains(search)
                || _db.TimePlanningEmployee.Any(pe => pe.PlanID == p.PlanID
                    && _db.Employees.Any(e => e.EmployeeId == pe.EmployeeID
                        && _db.People.Any(pp => pp.PersonId == e.PersonID
                            && (pp.IdCard.Contains(search) || (pp.LastName + " " + pp.FirstName).Contains(search))))));
        }

        var total = await plans.LongCountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var pageOfPlans = plans
            .OrderByDescending(x => x.StartDate).ThenByDescending(x => x.PlanID)
            .Skip((page - 1) * pageSize).Take(pageSize);

        var items = await (
            from p in pageOfPlans
            join st in _db.RefTypes.AsNoTracking() on p.PlanStatusTypeID equals st.TypeId into stJoin
            from st in stJoin.DefaultIfEmpty()
            join cb in _db.Employees.AsNoTracking() on p.CreatedBy equals (int?)cb.EmployeeId into cbJoin
            from cb in cbJoin.DefaultIfEmpty()
            join cp in _db.People.AsNoTracking() on (int?)cb.PersonID equals (int?)cp.PersonId into cpJoin
            from cp in cpJoin.DefaultIfEmpty()
            orderby p.StartDate descending, p.PlanID descending
            select new TimePlanningOverviewItemDto(
                p.PlanID,
                p.PlanType,
                p.Title,
                p.Description,
                p.StartDate,
                p.EndDate,
                p.StartTime,
                p.EndTime,
                p.OvertimeType,
                p.Factor,
                p.PlanStatusTypeID,
                st != null ? st.Name : string.Empty,
                cp != null ? cp.LastName + " " + cp.FirstName : string.Empty,
                p.CreatedAt,
                _db.TimePlanningEmployee.Count(pe => pe.PlanID == p.PlanID),
                _db.TimePlanningEmployee.Where(pe => pe.PlanID == p.PlanID).Sum(pe => (decimal?)pe.AssignedHours) ?? 0m,
                _db.TimePlanningEmployee.Where(pe => pe.PlanID == p.PlanID).Sum(pe => (decimal?)pe.ActualHours) ?? 0m)
        ).ToListAsync(ct);

        return new PagedResult<TimePlanningOverviewItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimePlanningOverviewExecutionDto>> GetPlanEmployeeExecutionsAsync(
        int planEmployeeId, IReadOnlyList<int>? allowedDepartmentIds, CancellationToken ct = default)
    {
        var allowed = allowedDepartmentIds?.ToList();
        if (allowed is not null)
        {
            var inScope = await (
                from pe in _db.TimePlanningEmployee.AsNoTracking()
                where pe.PlanEmployeeID == planEmployeeId
                join e in _db.Employees.AsNoTracking() on pe.EmployeeID equals e.EmployeeId
                select e.DepartmentId).FirstOrDefaultAsync(ct);
            if (inScope is null || !allowed.Contains(inScope.Value)) return [];
        }

        // Proyección explícita: solo lee las columnas del DTO y no depende de navegaciones del modelo.
        return await (
            from ex in _db.TimePlanningExecution.AsNoTracking()
            where ex.PlanEmployeeID == planEmployeeId
            join vb in _db.Employees.AsNoTracking() on ex.VerifiedBy equals (int?)vb.EmployeeId into vbJoin
            from vb in vbJoin.DefaultIfEmpty()
            join vp in _db.People.AsNoTracking() on (int?)vb.PersonID equals (int?)vp.PersonId into vpJoin
            from vp in vpJoin.DefaultIfEmpty()
            orderby ex.WorkDate, ex.StartTime
            select new TimePlanningOverviewExecutionDto(
                ex.ExecutionID, ex.WorkDate, ex.StartTime, ex.EndTime, ex.TotalMinutes, ex.RegularMinutes,
                ex.OvertimeMinutes, ex.NightMinutes, ex.HolidayMinutes, ex.ExceededMinutes,
                vp != null ? vp.LastName + " " + vp.FirstName : null, ex.VerifiedAt, ex.Comments)
        ).ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<PagedResult<TimePlanningOverviewEmployeeDto>> GetPlanEmployeesPagedAsync(
        int planId, int page, int pageSize, int? employeeStatusTypeId, string? search,
        IReadOnlyList<int>? allowedDepartmentIds, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var term = search?.Trim();
        var allowed = allowedDepartmentIds?.ToList();

        var rows = from pe in _db.TimePlanningEmployee.AsNoTracking()
                   where pe.PlanID == planId
                   join e in _db.Employees.AsNoTracking() on pe.EmployeeID equals e.EmployeeId
                   join pp in _db.People.AsNoTracking() on e.PersonID equals pp.PersonId
                   join d in _db.Departments.AsNoTracking() on e.DepartmentId equals (int?)d.DepartmentId into dJoin
                   from d in dJoin.DefaultIfEmpty()
                   join st in _db.RefTypes.AsNoTracking() on pe.EmployeeStatusTypeID equals st.TypeId into stJoin
                   from st in stJoin.DefaultIfEmpty()
                   select new { pe, e, pp, d, st };

        if (employeeStatusTypeId.HasValue)
            rows = rows.Where(x => x.pe.EmployeeStatusTypeID == employeeStatusTypeId.Value);
        if (allowed is not null)
            rows = rows.Where(x => x.e.DepartmentId != null && allowed.Contains(x.e.DepartmentId.Value));
        if (!string.IsNullOrEmpty(term))
            rows = rows.Where(x => x.pp.IdCard.Contains(term) || (x.pp.LastName + " " + x.pp.FirstName).Contains(term));

        var total = await rows.LongCountAsync(ct);

        var items = await rows
            .OrderBy(x => x.pp.LastName).ThenBy(x => x.pp.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TimePlanningOverviewEmployeeDto(
                x.pe.PlanEmployeeID,
                x.pe.PlanID,
                x.pe.EmployeeID,
                x.pp.LastName + " " + x.pp.FirstName,
                x.pp.IdCard,
                x.d != null ? x.d.Name : string.Empty,
                x.pe.AssignedHours,
                x.pe.ActualHours,
                x.pe.EmployeeStatusTypeID,
                x.st != null ? x.st.Name : string.Empty,
                x.pe.IsEligible,
                _db.TimePlanningExecution.Count(ex => ex.PlanEmployeeID == x.pe.PlanEmployeeID),
                _db.TimePlanningExecution.Where(ex => ex.PlanEmployeeID == x.pe.PlanEmployeeID).Sum(ex => (int?)ex.OvertimeMinutes) ?? 0))
            .ToListAsync(ct);

        return new PagedResult<TimePlanningOverviewEmployeeDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}
