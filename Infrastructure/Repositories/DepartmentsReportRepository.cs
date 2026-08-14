using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IDepartmentsReportRepository"/> usando EF Core + LINQ.
/// No depende de HR.tbl_Faculties (tabla obsoleta, ya no existe en el esquema real) —
/// el reporte se arma únicamente con columnas vigentes.
/// </summary>
public sealed class DepartmentsReportRepository : IDepartmentsReportRepository
{
    private readonly AppDbContext _db;

    public DepartmentsReportRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DepartmentReportDto>> GetDepartmentsDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var includeInactive = filter.IncludeInactive ?? false;

        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => includeInactive || d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new { d.DepartmentId, d.Name, d.Code, d.IsActive, d.CreatedAt, d.UpdatedAt, d.DepartmentType, d.DepartmentScope, d.ParentId })
            .ToListAsync(ct);

        if (departments.Count == 0) return [];

        // Tipo/Ámbito (ref_Types) y nombre de la dependencia padre — resueltos aparte
        // porque no son traducibles a un único Select() sin duplicar el join por cada
        // categoría distinta de ref_Types.
        var refTypeIds = departments
            .SelectMany(d => new[] { d.DepartmentType, d.DepartmentScope })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var refTypeNames = await _db.RefTypes.AsNoTracking()
            .Where(r => refTypeIds.Contains(r.TypeId))
            .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);

        var parentIds = departments.Where(d => d.ParentId.HasValue).Select(d => d.ParentId!.Value).Distinct().ToList();
        var parentNames = await _db.Departments.AsNoTracking()
            .Where(d => parentIds.Contains(d.DepartmentId))
            .ToDictionaryAsync(d => d.DepartmentId, d => d.Name, ct);

        var deptIds = departments.Select(d => d.DepartmentId).ToList();

        var employees = await _db.Employees
            .AsNoTracking()
            .Where(e => e.DepartmentId.HasValue && deptIds.Contains(e.DepartmentId.Value))
            .Select(e => new { e.EmployeeId, e.PersonID, e.DepartmentId, e.IsActive })
            .ToListAsync(ct);

        var personIds = employees.Select(e => e.PersonID).Distinct().ToList();

        // "Top 1 por grupo" no es traducible a SQL por EF Core — se trae a memoria
        // el conjunto acotado (contratos de estas personas) y se agrupa en LINQ-to-Objects.
        var contractsByPerson = await _db.Set<Contracts>()
            .AsNoTracking()
            .Where(c => personIds.Contains(c.PersonID))
            .Select(c => new { c.PersonID, c.ContractID, c.StartDate })
            .ToListAsync(ct);

        var latestContractByPerson = contractsByPerson
            .GroupBy(c => c.PersonID)
            .Select(g => g.OrderByDescending(c => c.StartDate).ThenByDescending(c => c.ContractID).First())
            .ToList();

        var contractIds = latestContractByPerson.Select(c => c.ContractID).ToList();

        var salariesByContract = await _db.SalaryHistory
            .AsNoTracking()
            .Where(s => s.ContractId.HasValue && contractIds.Contains(s.ContractId.Value))
            .Select(s => new { ContractId = s.ContractId!.Value, s.NewSalary, s.ChangedAt })
            .ToListAsync(ct);

        var latestSalaryByContract = salariesByContract
            .GroupBy(s => s.ContractId)
            .Select(g => g.OrderByDescending(s => s.ChangedAt).First())
            .ToList();

        var salaryByPerson = latestContractByPerson
            .Join(latestSalaryByContract, c => c.ContractID, s => s.ContractId, (c, s) => new { c.PersonID, s.NewSalary })
            .ToDictionary(x => x.PersonID, x => x.NewSalary);

        var result = new List<DepartmentReportDto>(departments.Count);
        foreach (var dept in departments)
        {
            var deptEmployees = employees.Where(e => e.DepartmentId == dept.DepartmentId).ToList();
            var salaries = deptEmployees
                .Where(e => salaryByPerson.ContainsKey(e.PersonID))
                .Select(e => salaryByPerson[e.PersonID])
                .ToList();

            result.Add(new DepartmentReportDto
            {
                Id                = dept.DepartmentId,
                DepartmentName    = dept.Name,
                DepartmentCode    = dept.Code,
                FacultyName       = string.Empty, // HR.tbl_Faculties ya no existe
                FacultyCode       = string.Empty,
                DepartmentTypeName  = dept.DepartmentType.HasValue && refTypeNames.TryGetValue(dept.DepartmentType.Value, out var dtName) ? dtName : null,
                DepartmentScopeName = dept.DepartmentScope.HasValue && refTypeNames.TryGetValue(dept.DepartmentScope.Value, out var dsName) ? dsName : null,
                ParentDepartmentName = dept.ParentId.HasValue && parentNames.TryGetValue(dept.ParentId.Value, out var pName) ? pName : null,
                IsActive          = dept.IsActive,
                TotalEmployees    = deptEmployees.Count,
                ActiveEmployees   = deptEmployees.Count(e => e.IsActive),
                InactiveEmployees = deptEmployees.Count(e => !e.IsActive),
                AverageSalary     = salaries.Count > 0 ? salaries.Average() : 0,
                TotalSalaries     = salaries.Sum(),
                MinSalary         = salaries.Count > 0 ? salaries.Min() : 0,
                MaxSalary         = salaries.Count > 0 ? salaries.Max() : 0,
                CreatedAt         = dept.CreatedAt ?? default,
                UpdatedAt         = dept.UpdatedAt ?? default,
            });
        }

        return result.AsReadOnly();
    }
}
