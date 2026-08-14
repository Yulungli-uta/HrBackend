using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.EmployeeInternalRequest;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

// ReSharper disable MethodHasAsyncOverload

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class EmployeeInternalRequestRepository : IEmployeeInternalRequestRepository
{
    private readonly AppDbContext _db;

    public EmployeeInternalRequestRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public Task<EmployeeInternalRequest?> GetTrackedByIdAsync(int requestId, CancellationToken ct = default)
        => _db.EmployeeInternalRequests.FirstOrDefaultAsync(r => r.RequestId == requestId, ct);

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto?> GetDetailByIdAsync(int requestId, CancellationToken ct = default)
    {
        var row = await (
            from r in _db.EmployeeInternalRequests.AsNoTracking()
            where r.RequestId == requestId
            join emp in _db.Employees.AsNoTracking() on r.EmployeeId equals emp.EmployeeId
            join person in _db.People.AsNoTracking() on emp.PersonID equals person.PersonId
            join dept in _db.Departments.AsNoTracking() on emp.DepartmentId equals dept.DepartmentId into deptLeft
            from dept in deptLeft.DefaultIfEmpty()
            select new
            {
                r.RequestId,
                r.EmployeeId,
                EmployeeFullName = person.LastName + " " + person.FirstName,
                EmployeeIdCard = person.IdCard,
                emp.DepartmentId,
                DepartmentName = dept != null ? dept.Name : null,
                r.RequestType,
                r.Subject,
                r.Description,
                r.Status,
                r.CreatedAt,
                r.CreatedBy,
                r.UpdatedAt,
                r.ResolvedAt,
                r.ResolvedBy,
                r.CancelledAt,
                r.CancelledBy,
                r.RowVersion
            }
        ).FirstOrDefaultAsync(ct);

        if (row is null) return null;

        var history = await _db.EmployeeInternalRequestStatusHistories.AsNoTracking()
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new EmployeeInternalRequestStatusHistoryDto(
                h.HistoryId, h.RequestId, h.PreviousStatus, h.NewStatus, h.Action, h.Observation, h.CreatedAt, h.CreatedBy))
            .ToListAsync(ct);

        string? resolvedByName = null;
        if (row.ResolvedBy.HasValue)
        {
            resolvedByName = await (
                from emp in _db.Employees.AsNoTracking()
                join person in _db.People.AsNoTracking() on emp.PersonID equals person.PersonId
                where emp.EmployeeId == row.ResolvedBy.Value
                select person.LastName + " " + person.FirstName
            ).FirstOrDefaultAsync(ct);
        }

        return new EmployeeInternalRequestDetailDto(
            row.RequestId, row.EmployeeId, row.EmployeeFullName, row.EmployeeIdCard, row.DepartmentId, row.DepartmentName,
            row.RequestType, row.Subject, row.Description, row.Status,
            row.CreatedAt, row.CreatedBy, row.UpdatedAt,
            row.ResolvedAt, row.ResolvedBy, resolvedByName,
            row.CancelledAt, row.CancelledBy,
            history, row.RowVersion ?? []);
    }

    /// <inheritdoc/>
    public async Task<PagedEmployeeInternalRequestResult> GetPagedAsync(EmployeeInternalRequestQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query =
            from r in _db.EmployeeInternalRequests.AsNoTracking()
            join emp in _db.Employees.AsNoTracking() on r.EmployeeId equals emp.EmployeeId
            join person in _db.People.AsNoTracking() on emp.PersonID equals person.PersonId
            join dept in _db.Departments.AsNoTracking() on emp.DepartmentId equals dept.DepartmentId into deptLeft
            from dept in deptLeft.DefaultIfEmpty()
            select new { r, person, emp.DepartmentId, DepartmentName = dept != null ? dept.Name : null };

        if (filter.EmployeeId.HasValue)
            query = query.Where(x => x.r.EmployeeId == filter.EmployeeId.Value);

        if (!string.IsNullOrWhiteSpace(filter.RequestType))
            query = query.Where(x => x.r.RequestType == filter.RequestType);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.r.Status == filter.Status);

        if (filter.AllowedDepartmentIds is { Count: > 0 } allowedDeptIds)
            query = query.Where(x => x.DepartmentId.HasValue && allowedDeptIds.Contains(x.DepartmentId.Value));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EmployeeInternalRequestSummaryDto(
                x.r.RequestId, x.r.EmployeeId, x.person.LastName + " " + x.person.FirstName, x.person.IdCard,
                x.DepartmentName, x.r.RequestType, x.r.Subject, x.r.Status, x.r.CreatedAt))
            .ToListAsync(ct);

        var totalPages = filter.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)filter.PageSize) : 0;
        return new PagedEmployeeInternalRequestResult(items, totalCount, filter.Page, filter.PageSize, totalPages);
    }

    /// <inheritdoc/>
    public async Task AddAsync(EmployeeInternalRequest entity, CancellationToken ct = default)
        => await _db.EmployeeInternalRequests.AddAsync(entity, ct);

    /// <inheritdoc/>
    public async Task AddHistoryAsync(EmployeeInternalRequestStatusHistory history, CancellationToken ct = default)
        => await _db.EmployeeInternalRequestStatusHistories.AddAsync(history, ct);

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
