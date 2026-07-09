using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.ResignationRetirement;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

// ReSharper disable MethodHasAsyncOverload

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IResignationRetirementRepository"/> usando EF Core + LINQ.
/// Reutiliza Employees/People/Departments/Job/RefTypes/EmployeeLaborRegime/Contracts/
/// PersonnelActions/TimeBalances existentes — no duplica datos personales ni laborales.
/// </summary>
public sealed class ResignationRetirementRepository : IResignationRetirementRepository
{
    private const string DocumentTypeContract = "CONTRACT";
    private const string DocumentTypePersonnelAction = "PERSONNEL_ACTION";

    private readonly AppDbContext _db;

    public ResignationRetirementRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<EmployeeConsolidatedInfoDto?> GetEmployeeConsolidatedInfoAsync(int employeeId, CancellationToken ct = default)
    {
        var employee = await (
            from emp in _db.Employees.AsNoTracking()
            where emp.EmployeeId == employeeId
            join person in _db.People.AsNoTracking() on emp.PersonID equals person.PersonId
            join dept in _db.Departments.AsNoTracking() on emp.DepartmentId equals dept.DepartmentId into deptLeft
            from dept in deptLeft.DefaultIfEmpty()
            join job in _db.Jobs.AsNoTracking() on emp.JobId equals job.JobID into jobLeft
            from job in jobLeft.DefaultIfEmpty()
            select new
            {
                emp.EmployeeId,
                person.PersonId,
                person.IdCard,
                FirstName = person.FirstName,
                LastName = person.LastName,
                person.Email,
                PersonalEmail = person.Email,
                person.Phone,
                person.BirthDate,
                emp.HireDate,
                emp.ImmediateBossId,
                DepartmentId = dept != null ? (int?)dept.DepartmentId : null,
                DepartmentName = dept != null ? dept.Name : null,
                JobTitle = job != null ? job.Description : null,
                EmployeeEmail = emp.Email
            }
        ).FirstOrDefaultAsync(ct);

        if (employee is null) return null;

        string? bossName = null;
        if (employee.ImmediateBossId.HasValue)
        {
            bossName = await (
                from bossEmp in _db.Employees.AsNoTracking()
                where bossEmp.EmployeeId == employee.ImmediateBossId.Value
                join bossPerson in _db.People.AsNoTracking() on bossEmp.PersonID equals bossPerson.PersonId
                select bossPerson.FirstName + " " + bossPerson.LastName
            ).FirstOrDefaultAsync(ct);
        }

        // ── Régimen vigente: fuente única de verdad HR.tbl_EmployeeLaborRegime ───
        // (evita depender de Employees.LaborRegimeId / Contracts.LaborRegimeID, que
        // pueden estar sin poblar — ver Database/MULTI_REGIME_EMPLOYEES.md). Un
        // empleado multi-régimen puede tener varias filas activas; IsPrincipal=1
        // marca cuál es "la vigente" para efectos de este módulo.
        var principalRegime = await (
            from lr in _db.EmployeeLaborRegimes.AsNoTracking()
            where lr.EmployeeId == employeeId && lr.IsActive
            join regimeType in _db.RefTypes.AsNoTracking() on lr.LaborRegimeId equals regimeType.TypeId into regimeLeft
            from regimeType in regimeLeft.DefaultIfEmpty()
            join dept in _db.Departments.AsNoTracking() on lr.DepartmentId equals dept.DepartmentId into deptLeft
            from dept in deptLeft.DefaultIfEmpty()
            join job in _db.Jobs.AsNoTracking() on lr.JobId equals job.JobID into jobLeft
            from job in jobLeft.DefaultIfEmpty()
            orderby lr.IsPrincipal descending
            select new
            {
                lr.LaborRegimeId,
                RegimeName = regimeType != null ? regimeType.Name : null,
                DepartmentName = dept != null ? dept.Name : null,
                JobTitle = job != null ? job.Description : null,
                lr.DocumentType,
                lr.DocumentNumber,
                lr.SourceContractId,
                lr.SourcePersonnelActionId
            }
        ).FirstOrDefaultAsync(ct);

        string? vigenteSourceType = null;
        int? vigenteSourceId = null;
        DateOnly? vigenteStart = null;
        DateOnly? vigenteEnd = null;

        if (principalRegime?.DocumentType == DocumentTypeContract && principalRegime.SourceContractId.HasValue)
        {
            vigenteSourceType = DocumentTypeContract;
            vigenteSourceId = principalRegime.SourceContractId;
            var contractDates = await _db.Contracts.AsNoTracking()
                .Where(c => c.ContractID == principalRegime.SourceContractId.Value)
                .Select(c => new { c.StartDate, c.EndDate })
                .FirstOrDefaultAsync(ct);
            if (contractDates is not null)
            {
                vigenteStart = DateOnly.FromDateTime(contractDates.StartDate);
                vigenteEnd = DateOnly.FromDateTime(contractDates.EndDate);
            }
        }
        else if (principalRegime?.DocumentType == DocumentTypePersonnelAction && principalRegime.SourcePersonnelActionId.HasValue)
        {
            vigenteSourceType = DocumentTypePersonnelAction;
            vigenteSourceId = principalRegime.SourcePersonnelActionId;
            var actionDates = await _db.PersonnelActions.AsNoTracking()
                .Where(a => a.ActionId == principalRegime.SourcePersonnelActionId.Value)
                .Select(a => new { a.EffectiveDate, a.EndDate })
                .FirstOrDefaultAsync(ct);
            if (actionDates is not null)
            {
                vigenteStart = actionDates.EffectiveDate;
                vigenteEnd = actionDates.EndDate;
            }
        }

        var vacationMinutes = await _db.TimeBalances.AsNoTracking()
            .Where(t => t.EmployeeID == employeeId)
            .SumAsync(t => (int?)t.VacationAvailableMin, ct) ?? 0;
        var vacationDays = Math.Round(vacationMinutes / 480m, 1); // jornada de 8h = 480min

        var serviceSpan = DateOnly.FromDateTime(DateTime.Today).DayNumber - employee.HireDate.DayNumber;
        var serviceDate = employee.HireDate.AddDays(0);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var years = today.Year - employee.HireDate.Year;
        var months = today.Month - employee.HireDate.Month;
        if (today.Day < employee.HireDate.Day) months--;
        if (months < 0) { years--; months += 12; }
        if (serviceSpan < 0) { years = 0; months = 0; }

        int? age = null;
        if (employee.BirthDate.HasValue)
        {
            var birth = employee.BirthDate.Value;
            age = today.Year - birth.Year;
            if (today.Month < birth.Month || (today.Month == birth.Month && today.Day < birth.Day)) age--;
        }

        return new EmployeeConsolidatedInfoDto(
            EmployeeId: employee.EmployeeId,
            PersonId: employee.PersonId,
            IdCard: employee.IdCard,
            FullName: $"{employee.FirstName} {employee.LastName}",
            Email: employee.EmployeeEmail ?? employee.Email,
            PersonalEmail: employee.PersonalEmail,
            Phone: employee.Phone,
            JobTitle: employee.JobTitle,
            DepartmentId: employee.DepartmentId,
            DepartmentName: employee.DepartmentName,
            LaborRegimeTypeId: principalRegime?.LaborRegimeId,
            LaborRegimeName: principalRegime?.RegimeName,
            ContractTypeName: principalRegime?.RegimeName,
            HireDate: employee.HireDate,
            ImmediateBossId: employee.ImmediateBossId,
            ImmediateBossName: bossName,
            VigenteSourceType: vigenteSourceType,
            VigenteSourceId: vigenteSourceId,
            VigenteDocumentNumber: principalRegime?.DocumentNumber,
            VigenteStartDate: vigenteStart,
            VigenteEndDate: vigenteEnd,
            VigenteJobTitle: principalRegime?.JobTitle,
            VigenteDepartmentName: principalRegime?.DepartmentName,
            VacationAvailableDays: vacationDays,
            ServiceTimeYears: years,
            ServiceTimeMonths: months,
            Age: age,
            IsRetirementEligible: false,
            RetirementEligibilityNote: null
        );
    }

    /// <inheritdoc/>
    public Task<int?> GetPublishedTemplateIdAsync(string templateCode, CancellationToken ct = default)
        => _db.DocumentTemplates.AsNoTracking()
            .Where(t => t.TemplateCode == templateCode && t.Status == DocumentTemplateStatus.Published)
            .Select(t => (int?)t.TemplateId)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<bool> HasActiveRequestAsync(int employeeId, string requestType, int? excludeRequestId = null, CancellationToken ct = default)
    {
        var activeStatuses = new[]
        {
            ResignationRetirementStatus.Pendiente,
            ResignationRetirementStatus.EnRevision,
            ResignationRetirementStatus.Devuelto
        };

        return await _db.ResignationRetirementRequests.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId
                        && r.RequestType == requestType
                        && activeStatuses.Contains(r.Status)
                        && (excludeRequestId == null || r.RequestId != excludeRequestId.Value))
            .AnyAsync(ct);
    }

    /// <inheritdoc/>
    public Task<ResignationRetirementRequest?> GetTrackedByIdAsync(int requestId, CancellationToken ct = default)
        => _db.ResignationRetirementRequests.FirstOrDefaultAsync(r => r.RequestId == requestId, ct);

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto?> GetDetailByIdAsync(int requestId, CancellationToken ct = default)
    {
        var request = await _db.ResignationRetirementRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RequestId == requestId, ct);
        if (request is null) return null;

        var employeeInfo = await GetEmployeeConsolidatedInfoAsync(request.EmployeeId, ct);
        if (employeeInfo is null) return null;

        var history = await GetHistoryAsync(requestId, ct);

        var actorIds = new[] { request.CreatedBy, request.ApprovedBy, request.RejectedBy, request.CancelledBy }
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        var actorNames = await (
            from emp in _db.Employees.AsNoTracking()
            join person in _db.People.AsNoTracking() on emp.PersonID equals person.PersonId
            where actorIds.Contains(emp.EmployeeId)
            select new { emp.EmployeeId, FullName = person.FirstName + " " + person.LastName }
        ).ToDictionaryAsync(x => x.EmployeeId, x => x.FullName, ct);

        string? generatedDocumentFileName = null;
        if (request.GeneratedDocumentId.HasValue)
        {
            generatedDocumentFileName = await _db.GeneratedDocuments.AsNoTracking()
                .Where(d => d.DocumentId == request.GeneratedDocumentId.Value)
                .Select(d => d.FileName)
                .FirstOrDefaultAsync(ct);
        }

        return new ResignationRetirementDetailDto(
            RequestId: request.RequestId,
            RequestType: request.RequestType,
            RequestDate: request.RequestDate,
            ProposedExitDate: request.ProposedExitDate,
            Reason: request.Reason,
            AdditionalNotes: request.AdditionalNotes,
            Status: request.Status,
            LinkedPersonnelActionId: request.LinkedPersonnelActionId,
            GeneratedDocumentId: request.GeneratedDocumentId,
            GeneratedDocumentFileName: generatedDocumentFileName,
            Employee: employeeInfo,
            CreatedAt: request.CreatedAt,
            CreatedBy: request.CreatedBy,
            CreatedByName: request.CreatedBy.HasValue ? actorNames.GetValueOrDefault(request.CreatedBy.Value) : null,
            UpdatedAt: request.UpdatedAt,
            UpdatedBy: request.UpdatedBy,
            ApprovedAt: request.ApprovedAt,
            ApprovedBy: request.ApprovedBy,
            ApprovedByName: request.ApprovedBy.HasValue ? actorNames.GetValueOrDefault(request.ApprovedBy.Value) : null,
            RejectedAt: request.RejectedAt,
            RejectedBy: request.RejectedBy,
            RejectedByName: request.RejectedBy.HasValue ? actorNames.GetValueOrDefault(request.RejectedBy.Value) : null,
            CancelledAt: request.CancelledAt,
            CancelledBy: request.CancelledBy,
            CancelledByName: request.CancelledBy.HasValue ? actorNames.GetValueOrDefault(request.CancelledBy.Value) : null,
            History: history,
            RowVersion: request.RowVersion ?? []
        );
    }

    /// <inheritdoc/>
    public async Task<PagedResignationRetirementResult> GetPagedAsync(ResignationRetirementQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query =
            from r in _db.ResignationRetirementRequests.AsNoTracking()
            join emp in _db.Employees.AsNoTracking() on r.EmployeeId equals emp.EmployeeId
            join person in _db.People.AsNoTracking() on emp.PersonID equals person.PersonId
            join dept in _db.Departments.AsNoTracking() on emp.DepartmentId equals dept.DepartmentId into deptLeft
            from dept in deptLeft.DefaultIfEmpty()
            select new { r, person, emp, DepartmentName = dept != null ? dept.Name : null, emp.DepartmentId };

        if (filter.EmployeeId.HasValue)
            query = query.Where(x => x.r.EmployeeId == filter.EmployeeId.Value);

        if (!string.IsNullOrWhiteSpace(filter.RequestType))
            query = query.Where(x => x.r.RequestType == filter.RequestType);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.r.Status == filter.Status);

        if (filter.DateFrom.HasValue)
            query = query.Where(x => x.r.RequestDate >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(x => x.r.RequestDate <= filter.DateTo.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);

        if (filter.AllowedDepartmentIds is { Count: > 0 } allowedDeptIds)
            query = query.Where(x => x.DepartmentId.HasValue && allowedDeptIds.Contains(x.DepartmentId.Value));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new ResignationRetirementSummaryDto(
                x.r.RequestId,
                x.r.EmployeeId,
                x.person.FirstName + " " + x.person.LastName,
                x.person.IdCard,
                x.DepartmentName,
                x.r.RequestType,
                x.r.RequestDate,
                x.r.ProposedExitDate,
                x.r.Status,
                x.r.CreatedAt
            ))
            .ToListAsync(ct);

        var totalPages = filter.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)filter.PageSize) : 0;

        return new PagedResignationRetirementResult(items, totalCount, filter.Page, filter.PageSize, totalPages);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ResignationRetirementStatusHistoryDto>> GetHistoryAsync(int requestId, CancellationToken ct = default)
        => await _db.ResignationRetirementStatusHistories.AsNoTracking()
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new ResignationRetirementStatusHistoryDto(
                h.HistoryId, h.RequestId, h.PreviousStatus, h.NewStatus, h.Action, h.Observation, h.CreatedAt, h.CreatedBy))
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task AddAsync(ResignationRetirementRequest entity, CancellationToken ct = default)
        => await _db.ResignationRetirementRequests.AddAsync(entity, ct);

    /// <inheritdoc/>
    public async Task AddHistoryAsync(ResignationRetirementStatusHistory history, CancellationToken ct = default)
        => await _db.ResignationRetirementStatusHistories.AddAsync(history, ct);

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
