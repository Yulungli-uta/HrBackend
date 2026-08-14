using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.EmployeeCertificate;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

// ReSharper disable MethodHasAsyncOverload

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class EmployeeCertificateRepository : IEmployeeCertificateRepository
{
    private readonly AppDbContext _db;

    public EmployeeCertificateRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public Task<int?> GetPublishedTemplateIdAsync(string templateCode, CancellationToken ct = default)
        => _db.DocumentTemplates.AsNoTracking()
            .Where(t => t.TemplateCode == templateCode && t.Status == DocumentTemplateStatus.Published)
            .Select(t => (int?)t.TemplateId)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<(string? JobDescription, string? DepartmentName, int? DepartmentId)> GetCurrentPositionAsync(int employeeId, CancellationToken ct = default)
    {
        var row = await (
            from emp in _db.Employees.AsNoTracking()
            where emp.EmployeeId == employeeId
            join dept in _db.Departments.AsNoTracking() on emp.DepartmentId equals dept.DepartmentId into deptLeft
            from dept in deptLeft.DefaultIfEmpty()
            join job in _db.Jobs.AsNoTracking() on emp.JobId equals job.JobID into jobLeft
            from job in jobLeft.DefaultIfEmpty()
            select new
            {
                JobDescription = job != null ? job.Description : null,
                DepartmentName = dept != null ? dept.Name : null,
                DepartmentId = dept != null ? (int?)dept.DepartmentId : null
            }
        ).FirstOrDefaultAsync(ct);

        return row is null ? (null, null, null) : (row.JobDescription, row.DepartmentName, row.DepartmentId);
    }

    private const string ContractStatusCategory = "CONTRACT_STATUS";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmploymentHistoryEntry>> GetEmploymentHistoryAsync(int employeeId, CancellationToken ct = default)
    {
        var personId = await _db.Employees.AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .Select(e => (int?)e.PersonID)
            .FirstOrDefaultAsync(ct);

        var entries = new List<EmploymentHistoryEntry>();

        if (personId.HasValue)
        {
            var contracts = await (
                from c in _db.Contracts.AsNoTracking()
                where c.PersonID == personId.Value
                join dept in _db.Departments.AsNoTracking() on c.DepartmentID equals dept.DepartmentId into deptLeft
                from dept in deptLeft.DefaultIfEmpty()
                join job in _db.Jobs.AsNoTracking() on c.JobID equals job.JobID into jobLeft
                from job in jobLeft.DefaultIfEmpty()
                join statusType in _db.RefTypes.AsNoTracking()
                    on new { Id = c.Status, Cat = ContractStatusCategory } equals new { Id = statusType.TypeId, Cat = statusType.Category } into statusLeft
                from statusType in statusLeft.DefaultIfEmpty()
                orderby c.StartDate
                select new EmploymentHistoryEntry(
                    "CONTRACT",
                    c.ContractCode,
                    DateOnly.FromDateTime(c.StartDate),
                    (DateOnly?)DateOnly.FromDateTime(c.EndDate),
                    job != null ? job.Description : null,
                    dept != null ? dept.Name : null,
                    statusType != null ? statusType.Name : null)
            ).ToListAsync(ct);
            entries.AddRange(contracts);
        }

        var actions = await (
            from a in _db.PersonnelActions.AsNoTracking()
            where a.EmployeeId == employeeId
            join dept in _db.Departments.AsNoTracking() on a.DestinationDepartmentId equals dept.DepartmentId into deptLeft
            from dept in deptLeft.DefaultIfEmpty()
            join job in _db.Jobs.AsNoTracking() on a.DestinationJobId equals job.JobID into jobLeft
            from job in jobLeft.DefaultIfEmpty()
            orderby a.ActionDate
            select new EmploymentHistoryEntry(
                "PERSONNEL_ACTION",
                a.ActionNumber,
                a.EffectiveDate,
                a.EndDate,
                job != null ? job.Description : null,
                dept != null ? dept.Name : null,
                a.Status)
        ).ToListAsync(ct);
        entries.AddRange(actions);

        return entries.OrderBy(e => e.StartDate).ToList();
    }

    /// <inheritdoc/>
    public Task<EmployeeCertificateRequest?> GetTrackedByIdAsync(int requestId, CancellationToken ct = default)
        => _db.EmployeeCertificateRequests.FirstOrDefaultAsync(r => r.RequestId == requestId, ct);

    /// <inheritdoc/>
    public async Task<EmployeeCertificateDetailDto?> GetDetailByIdAsync(int requestId, CancellationToken ct = default)
    {
        var row = await (
            from r in _db.EmployeeCertificateRequests.AsNoTracking()
            where r.RequestId == requestId
            join emp in _db.Employees.AsNoTracking() on r.EmployeeId equals emp.EmployeeId
            join person in _db.People.AsNoTracking() on emp.PersonID equals person.PersonId
            join doc in _db.GeneratedDocuments.AsNoTracking() on r.GeneratedDocumentId equals doc.DocumentId into docLeft
            from doc in docLeft.DefaultIfEmpty()
            select new
            {
                r.RequestId,
                r.EmployeeId,
                EmployeeFullName = person.LastName + " " + person.FirstName,
                emp.DepartmentId,
                r.CertificateType,
                r.Purpose,
                r.Status,
                r.GeneratedDocumentId,
                GeneratedDocumentFileName = doc != null ? doc.FileName : null,
                r.CreatedAt,
                r.IssuedAt,
                r.RowVersion
            }
        ).FirstOrDefaultAsync(ct);

        if (row is null) return null;

        var history = await _db.EmployeeCertificateStatusHistories.AsNoTracking()
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new EmployeeCertificateStatusHistoryDto(
                h.HistoryId, h.RequestId, h.PreviousStatus, h.NewStatus, h.Action, h.Observation, h.CreatedAt, h.CreatedBy))
            .ToListAsync(ct);

        return new EmployeeCertificateDetailDto(
            row.RequestId, row.EmployeeId, row.EmployeeFullName, row.DepartmentId, row.CertificateType, row.Purpose, row.Status,
            row.GeneratedDocumentId, row.GeneratedDocumentFileName, row.CreatedAt, row.IssuedAt, history, row.RowVersion ?? []);
    }

    /// <inheritdoc/>
    public async Task<PagedEmployeeCertificateResult> GetPagedAsync(EmployeeCertificateQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query =
            from r in _db.EmployeeCertificateRequests.AsNoTracking()
            join emp in _db.Employees.AsNoTracking() on r.EmployeeId equals emp.EmployeeId
            select new { r, emp.DepartmentId };

        if (filter.EmployeeId.HasValue)
            query = query.Where(x => x.r.EmployeeId == filter.EmployeeId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.r.Status == filter.Status);

        if (filter.AllowedDepartmentIds is { Count: > 0 } allowedDeptIds)
            query = query.Where(x => x.DepartmentId.HasValue && allowedDeptIds.Contains(x.DepartmentId.Value));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EmployeeCertificateSummaryDto(
                x.r.RequestId, x.r.EmployeeId, x.r.CertificateType, x.r.Purpose, x.r.Status,
                x.r.GeneratedDocumentId, x.r.CreatedAt, x.r.IssuedAt))
            .ToListAsync(ct);

        var totalPages = filter.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)filter.PageSize) : 0;
        return new PagedEmployeeCertificateResult(items, totalCount, filter.Page, filter.PageSize, totalPages);
    }

    /// <inheritdoc/>
    public async Task AddAsync(EmployeeCertificateRequest entity, CancellationToken ct = default)
        => await _db.EmployeeCertificateRequests.AddAsync(entity, ct);

    /// <inheritdoc/>
    public async Task AddHistoryAsync(EmployeeCertificateStatusHistory history, CancellationToken ct = default)
        => await _db.EmployeeCertificateStatusHistories.AddAsync(history, ct);

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
