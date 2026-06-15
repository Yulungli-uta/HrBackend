using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.PersonnelActions;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

// ReSharper disable MethodHasAsyncOverload

namespace WsUtaSystem.Infrastructure.Repositories;

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
                    join empJoin in _db.Employees.AsNoTracking()
                        on action.EmployeeId equals (int?)empJoin.EmployeeId into empLeft
                    from emp in empLeft.DefaultIfEmpty()
                    join person in _db.People.AsNoTracking()
                        on action.PersonId equals person.PersonId
                    join actionType in _db.PersonnelActionTypes.AsNoTracking()
                        on action.ActionTypeId equals actionType.PersonnelActionTypeId
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
                x.action.EmployeeId ?? 0,
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
            join empJoin in _db.Employees.AsNoTracking()
                on action.EmployeeId equals (int?)empJoin.EmployeeId into empLeft
            from emp in empLeft.DefaultIfEmpty()
            join person in _db.People.AsNoTracking()
                on action.PersonId equals person.PersonId
            join dept in _db.Departments.AsNoTracking()
                on emp.DepartmentId equals dept.DepartmentId into deptJoin
            from dept in deptJoin.DefaultIfEmpty()
            join actionType in _db.PersonnelActionTypes.AsNoTracking()
                on action.ActionTypeId equals actionType.PersonnelActionTypeId
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

        // Resolver nombres y cargos de responsables desde la vista de empleados
        var dthDirector        = await ResolveEmployeeAsync(result.action.DthDirectorId, ct);
        var authorityNominator = await ResolveEmployeeAsync(result.action.AuthorityNominatorId, ct);
        var elaborator         = await ResolveEmployeeAsync(result.action.ElaboratorId, ct);
        var reviewer           = await ResolveEmployeeAsync(result.action.ReviewerId, ct);
        var registrar          = await ResolveEmployeeAsync(result.action.RegistrarId, ct);

        // Resolver nombres de ref_Types para clasificación de la acción
        var instProcessName = result.action.InstitutionalProcess.HasValue
            ? await _db.RefTypes.AsNoTracking()
                .Where(r => r.TypeId == result.action.InstitutionalProcess.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var mgmtLevelName = result.action.ManagementLevel.HasValue
            ? await _db.RefTypes.AsNoTracking()
                .Where(r => r.TypeId == result.action.ManagementLevel.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var employeeTypeName = result.action.EmployeeTypeId.HasValue
            ? await _db.RefTypes.AsNoTracking()
                .Where(r => r.TypeId == result.action.EmployeeTypeId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        return new PersonnelActionDetailDto(
            result.action.ActionId,
            result.action.EmployeeId ?? 0,
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
            result.action.SwornDeclaration,
            result.action.InstitutionalProcess,
            instProcessName,
            result.action.ManagementLevel,
            mgmtLevelName,
            result.action.EmployeeTypeId,
            employeeTypeName,
            result.action.GeneratedDocumentId,
            generatedDocFileName,
            result.action.ContractId,
            result.action.MovementId,
            result.action.DthDirectorId,
            dthDirector.Name,
            dthDirector.JobTitle,
            result.action.AuthorityNominatorId,
            authorityNominator.Name,
            authorityNominator.JobTitle,
            result.action.ElaboratorId,
            elaborator.Name,
            elaborator.JobTitle,
            result.action.ReviewerId,
            reviewer.Name,
            reviewer.JobTitle,
            result.action.RegistrarId,
            registrar.Name,
            registrar.JobTitle,
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
            join empJoin in _db.Employees.AsNoTracking()
                on action.EmployeeId equals (int?)empJoin.EmployeeId into empLeft
            from emp in empLeft.DefaultIfEmpty()
            join person in _db.People.AsNoTracking()
                on action.PersonId equals person.PersonId
            join actionType in _db.PersonnelActionTypes.AsNoTracking()
                on action.ActionTypeId equals actionType.PersonnelActionTypeId
            where action.EmployeeId == employeeId
            orderby action.ActionDate descending
            select new PersonnelActionSummaryDto(
                action.ActionId,
                action.EmployeeId ?? 0,
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
    public async Task UpdateStatusAsync(int actionId, string statusCode, CancellationToken ct = default)
    {
        // Resuelve el StatusTypeId desde catálogo para mantener consistencia con ref_Types
        var statusTypeId = await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == "PERSONNEL_ACTION_STATUS" && r.Name == statusCode && r.IsActive)
            .Select(r => (int?)r.TypeId)
            .FirstOrDefaultAsync(ct);

        await _db.PersonnelActions
            .Where(a => a.ActionId == actionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, statusCode)
                .SetProperty(a => a.StatusTypeId, statusTypeId)
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

    /// <inheritdoc/>
    public async Task LinkSignedDocumentAsync(int actionId, int storedFileId, CancellationToken ct = default)
    {
        await _db.PersonnelActions
            .Where(a => a.ActionId == actionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.SignedDocumentStoredFileId, storedFileId)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow),
            ct);
    }

    /// <inheritdoc/>
    public async Task AddStatusHistoryAsync(PersonnelActionStatusHistory entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Resuelve StatusTypeId desde catálogo si no fue establecido previamente
        if (!entry.StatusTypeId.HasValue && !string.IsNullOrWhiteSpace(entry.StatusCode))
        {
            var typeId = await _db.RefTypes
                .AsNoTracking()
                .Where(r => r.Category == "PERSONNEL_ACTION_STATUS"
                         && r.Name == entry.StatusCode
                         && r.IsActive)
                .Select(r => (int?)r.TypeId)
                .FirstOrDefaultAsync(ct);

            entry.StatusTypeId = typeId;
        }

        _db.PersonnelActionStatusHistories.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PersonnelActionStatusHistoryDto>> GetStatusHistoryAsync(int actionId, CancellationToken ct = default)
    {
        return await _db.PersonnelActionStatusHistories
            .AsNoTracking()
            .Where(h => h.ActionId == actionId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new PersonnelActionStatusHistoryDto(
                h.HistoryId,
                h.ActionId,
                h.StatusTypeId,
                h.FromStatus,
                h.StatusCode,
                h.Comment,
                h.ChangedBy,
                h.ChangedAt))
            .ToListAsync(ct);
    }

    private async Task<(string? Name, string? JobTitle)> ResolveEmployeeAsync(int? employeeId, CancellationToken ct)
    {
        if (!employeeId.HasValue) return (null, null);

        var row = await (
            from emp in _db.Employees.AsNoTracking()
            join person in _db.People.AsNoTracking()
                on emp.PersonID equals person.PersonId
            join job in _db.Jobs.AsNoTracking()
                on emp.JobId equals job.JobID into jobJoin
            from job in jobJoin.DefaultIfEmpty()
            where emp.EmployeeId == employeeId.Value
            select new
            {
                Name     = person.FirstName + " " + person.LastName,
                JobTitle = (string?)job.Description
            }
        ).FirstOrDefaultAsync(ct);

        return row is null ? (null, null) : (row.Name, row.JobTitle);
    }
}
