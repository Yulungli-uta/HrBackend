using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EmployeesRepository : ServiceAwareEfRepository<Employees, int>, IEmployeesRepository
{
    private readonly WsUtaSystem.Data.AppDbContext _db;

    public EmployeesRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
    {
        _db = db;
    }
    
    public async Task<IEnumerable<Employees>> GetSubordinatesByBossIdAsync(
        int bossId,
        CancellationToken ct = default)
    {
        return await _db.Set<Employees>()
            .AsNoTracking()
            .Where(e => e.ImmediateBossId == bossId && e.IsActive)
            .OrderBy(e => e.EmployeeId)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Employees>> GetByPersonIdAsync(int personId, CancellationToken ct = default)
    {
        return await _db.Set<Employees>()
             .AsNoTracking()
             .Where(e => e.PersonID == personId && e.IsActive)
             .OrderBy(e => e.EmployeeId)
             .ToListAsync(ct);
    }

    // ref_Types (Category='CONTRACT_STATUS', Name='VIGENTE') — ver Database/hr seeds.
    private const int ContractStatusVigente = 274;

    /// <inheritdoc/>
    public async Task<IEnumerable<EmployeeReportDto>> GetEmployeesReportDataAsync(
        int? departmentId,
        int? employeeType,
        bool? isActive,
        DateTime? hireDateFrom,
        DateTime? hireDateTo,
        int? laborRegimeId = null,
        CancellationToken ct = default)
    {
        var regimeNames = await _db.RefTypes.AsNoTracking()
            .Where(r => r.Category == "CONTRACT_TYPE")
            .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);

        var baseQuery =
            from e in _db.Employees.AsNoTracking()
            where !e.IsDeleted
            join p in _db.People.AsNoTracking() on e.PersonID equals p.PersonId
            join d in _db.Departments.AsNoTracking() on e.DepartmentId equals d.DepartmentId into deptJoin
            from d in deptJoin.DefaultIfEmpty()
            join j in _db.Jobs.AsNoTracking() on e.JobId equals j.JobID into jobJoin
            from j in jobJoin.DefaultIfEmpty()
            select new { e, p, d, j };

        if (departmentId.HasValue)
            baseQuery = baseQuery.Where(x => x.e.DepartmentId == departmentId);
        if (employeeType.HasValue)
            baseQuery = baseQuery.Where(x => x.e.EmployeeType == employeeType);
        if (isActive.HasValue)
            baseQuery = baseQuery.Where(x => x.e.IsActive == isActive);
        if (hireDateFrom.HasValue)
        {
            var from = DateOnly.FromDateTime(hireDateFrom.Value);
            baseQuery = baseQuery.Where(x => x.e.HireDate >= from);
        }
        if (hireDateTo.HasValue)
        {
            var to = DateOnly.FromDateTime(hireDateTo.Value);
            baseQuery = baseQuery.Where(x => x.e.HireDate <= to);
        }

        var employees = await baseQuery
            .OrderBy(x => x.p.LastName).ThenBy(x => x.p.FirstName).ThenBy(x => x.e.HireDate)
            .ToListAsync(ct);
        var personIds = employees.Select(x => x.p.PersonId).Distinct().ToList();

        // Sueldo: contrato más reciente por persona (prioriza Status=VIGENTE), luego
        // el último HR.tbl_SalaryHistory de ese contrato. No cubre sueldo por
        // nombramiento/acción de personal sin contrato asociado (ver doc del método).
        var contracts = await _db.Contracts.AsNoTracking()
            .Where(c => personIds.Contains(c.PersonID) && !c.IsDeleted)
            .Select(c => new { c.ContractID, c.PersonID, c.Status, c.StartDate, c.ContractTypeID })
            .ToListAsync(ct);

        var latestContractByPerson = contracts
            .GroupBy(c => c.PersonID)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(c => c.Status == ContractStatusVigente)
                      .ThenByDescending(c => c.StartDate)
                      .First());

        var contractIds = latestContractByPerson.Values.Select(c => c.ContractID).ToList();

        var salaryByContract = await _db.SalaryHistory.AsNoTracking()
            .Where(s => s.ContractId.HasValue && contractIds.Contains(s.ContractId.Value))
            .GroupBy(s => s.ContractId!.Value)
            .Select(g => new
            {
                ContractId = g.Key,
                NewSalary = g.OrderByDescending(s => s.SalaryHistoryId).First().NewSalary
            })
            .ToDictionaryAsync(x => x.ContractId, x => x.NewSalary, ct);

        var contractTypeNames = await _db.ContractType.AsNoTracking()
            .ToDictionaryAsync(c => c.ContractTypeId, c => c.Name, ct);

        return employees.Select(x =>
        {
            latestContractByPerson.TryGetValue(x.p.PersonId, out var contract);
            var salary = contract is not null && salaryByContract.TryGetValue(contract.ContractID, out var s) ? s : 0m;
            var contractTypeName = contract is not null && contractTypeNames.TryGetValue(contract.ContractTypeID, out var ctName)
                ? ctName
                : null;

            return new EmployeeReportDto
            {
                Id = x.e.EmployeeId,
                FullName = $"{x.p.LastName} {x.p.FirstName}",
                FirstName = x.p.FirstName,
                LastName = x.p.LastName,
                IdentificationNumber = x.p.IdCard,
                Email = x.e.Email ?? string.Empty,
                DepartmentName = x.d?.Name ?? string.Empty,
                DepartmentCode = x.d?.Code ?? string.Empty,
                FacultyName = string.Empty,
                EmployeeType = x.e.EmployeeType.HasValue && regimeNames.TryGetValue(x.e.EmployeeType.Value, out var rn)
                    ? rn
                    : x.e.EmployeeType?.ToString() ?? "Sin tipo",
                JobTitle = x.j?.Description,
                IsActive = x.e.IsActive,
                BaseSalary = salary,
                NetSalary = salary,
                ContractType = contractTypeName,
                ContractStartDate = contract?.StartDate,
                HireDate = x.e.HireDate.ToDateTime(TimeOnly.MinValue),
                CreatedAt = x.e.CreatedAt ?? DateTime.MinValue,
                UpdatedAt = x.e.UpdatedAt ?? DateTime.MinValue,
            };
        }).ToList();
    }
}
