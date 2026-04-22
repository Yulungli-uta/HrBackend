using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Documents.PersonnelActions;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories.Documents;

/// <summary>
/// Implementación de <see cref="IPersonnelActionRepository"/> usando EF Core + LINQ.
/// Proyección directa a DTOs con joins a Employees, People, Departments, Jobs y RefTypes.
/// </summary>
public sealed class PersonnelActionRepository : IPersonnelActionRepository
{
    private readonly AppDbContext _db;

    public PersonnelActionRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<PagedPersonnelActionResult> GetPagedAsync(PersonnelActionQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = from action in _db.PersonnelActions.AsNoTracking()
                    join emp in _db.Employees.AsNoTracking()
                        on action.EmployeeId equals emp.EmployeeId
                    join person in _db.People.AsNoTracking()
                        on emp.PersonID equals person.PersonId
                    join actionType in _db.RefTypes.AsNoTracking()
                        on action.ActionTypeId equals actionType.TypeId
                    select new { action, emp, person, actionType };

        if (filter.EmployeeId.HasValue)
            query = query.Where(x => x.action.EmployeeId == filter.EmployeeId.Value);

        if (filter.ActionTypeId.HasValue)
            query = query.Where(x => x.action.ActionTypeId == filter.ActionTypeId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.action.Status == filter.Status);

        if (filter.StartDate.HasValue)
            query = query.Where(x => x.action.ActionDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(x => x.action.ActionDate <= filter.EndDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.action.ActionDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new PersonnelActionSummaryDto(
                x.action.ActionId,
                x.action.EmployeeId,
                x.person.FirstName + " " + x.person.LastName,
                x.person.IdCard,
                x.action.ActionTypeId,
                x.actionType.Name,
                x.action.ActionNumber,
                x.action.ActionDate,
                x.action.EffectiveDate,
                x.action.EndDate,
                x.action.Status,
                x.action.GeneratedDocumentId,
                x.action.CreatedAt
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

        return new PagedPersonnelActionResult(items, totalCount, filter.Page, filter.PageSize, totalPages);
    }

    /// <inheritdoc/>
    public async Task<PersonnelActionDetailDto?> GetDetailByIdAsync(int actionId, CancellationToken ct = default)
    {
        var result = await (
            from action in _db.PersonnelActions.AsNoTracking()
            join emp in _db.Employees.AsNoTracking()
                on action.EmployeeId equals emp.EmployeeId
            join person in _db.People.AsNoTracking()
                on emp.PersonID equals person.PersonId
            join dept in _db.Departments.AsNoTracking()
                on emp.DepartmentId equals dept.DepartmentId into deptJoin
            from dept in deptJoin.DefaultIfEmpty()
            join actionType in _db.RefTypes.AsNoTracking()
                on action.ActionTypeId equals actionType.TypeId
            where action.ActionId == actionId
            select new { action, emp, person, dept, actionType }
        ).FirstOrDefaultAsync(ct);

        if (result is null) return null;

        // Resolver nombres de departamentos y cargos de origen y destino
        var originDeptName = result.action.OriginDepartmentId.HasValue
            ? await _db.Departments.AsNoTracking()
                .Where(d => d.DepartmentId == result.action.OriginDepartmentId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var destDeptName = result.action.DestinationDepartmentId.HasValue
            ? await _db.Departments.AsNoTracking()
                .Where(d => d.DepartmentId == result.action.DestinationDepartmentId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var originJobTitle = result.action.OriginJobId.HasValue
            ? await _db.Jobs.AsNoTracking()
                .Where(j => j.JobID == result.action.OriginJobId.Value)
                .Select(j => j.Description)
                .FirstOrDefaultAsync(ct)
            : null;

        var destJobTitle = result.action.DestinationJobId.HasValue
            ? await _db.Jobs.AsNoTracking()
                .Where(j => j.JobID == result.action.DestinationJobId.Value)
                .Select(j => j.Description)
                .FirstOrDefaultAsync(ct)
            : null;

        var generatedDocFileName = result.action.GeneratedDocumentId.HasValue
            ? await _db.GeneratedDocuments.AsNoTracking()
                .Where(d => d.DocumentId == result.action.GeneratedDocumentId.Value)
                .Select(d => d.FileName)
                .FirstOrDefaultAsync(ct)
            : null;

        return new PersonnelActionDetailDto(
            result.action.ActionId,
            result.action.EmployeeId,
            result.person.FirstName + " " + result.person.LastName,
            result.person.IdCard,
            result.dept?.Name ?? string.Empty,
            string.Empty, // JobTitle se resuelve desde el contrato activo si se necesita
            result.action.ActionTypeId,
            result.actionType.Name,
            result.action.ActionNumber,
            result.action.ActionDate,
            result.action.EffectiveDate,
            result.action.EndDate,
            result.action.OriginDepartmentId,
            originDeptName,
            result.action.OriginJobId,
            originJobTitle,
            result.action.OriginBudgetCode,
            result.action.DestinationDepartmentId,
            destDeptName,
            result.action.DestinationJobId,
            destJobTitle,
            result.action.DestinationBudgetCode,
            result.action.PreviousRmu,
            result.action.NewRmu,
            result.action.LegalBasis,
            result.action.Reason,
            result.action.Observations,
            result.action.Status,
            result.action.GeneratedDocumentId,
            generatedDocFileName,
            result.action.ContractId,
            result.action.MovementId,
            result.action.CreatedAt,
            result.action.CreatedBy,
            result.action.UpdatedAt,
            result.action.UpdatedBy
        );
    }

    /// <inheritdoc/>
    public async Task<PersonnelAction?> GetByIdAsync(int actionId, CancellationToken ct = default)
        => await _db.PersonnelActions
            .FirstOrDefaultAsync(a => a.ActionId == actionId, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PersonnelActionSummaryDto>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await (
            from action in _db.PersonnelActions.AsNoTracking()
            join emp in _db.Employees.AsNoTracking()
                on action.EmployeeId equals emp.EmployeeId
            join person in _db.People.AsNoTracking()
                on emp.PersonID equals person.PersonId
            join actionType in _db.RefTypes.AsNoTracking()
                on action.ActionTypeId equals actionType.TypeId
            where action.EmployeeId == employeeId
            orderby action.ActionDate descending
            select new PersonnelActionSummaryDto(
                action.ActionId,
                action.EmployeeId,
                person.FirstName + " " + person.LastName,
                person.IdCard,
                action.ActionTypeId,
                actionType.Name,
                action.ActionNumber,
                action.ActionDate,
                action.EffectiveDate,
                action.EndDate,
                action.Status,
                action.GeneratedDocumentId,
                action.CreatedAt
            )
        ).ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<int> CreateAsync(PersonnelAction action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        _db.PersonnelActions.Add(action);
        await _db.SaveChangesAsync(ct);
        return action.ActionId;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(PersonnelAction action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        _db.PersonnelActions.Update(action);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpdateStatusAsync(int actionId, string status, CancellationToken ct = default)
    {
        await _db.PersonnelActions
            .Where(a => a.ActionId == actionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, status)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow),
            ct);
    }

    /// <inheritdoc/>
    public async Task LinkDocumentAsync(int actionId, int documentId, CancellationToken ct = default)
    {
        await _db.PersonnelActions
            .Where(a => a.ActionId == actionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.GeneratedDocumentId, documentId)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow),
            ct);
    }
}
