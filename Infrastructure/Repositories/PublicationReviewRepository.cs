using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Publications;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class PublicationReviewRepository : IPublicationReviewRepository
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;

    public PublicationReviewRepository(AppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<PagedResult<PublicationReviewRow>> GetPageAsync(
        PublicationReviewFilterDto filter, IReadOnlyCollection<int>? providerTypeIds, CancellationToken ct)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var query = ApplyFilter(_db.Publications.AsNoTracking(), filter, providerTypeIds);
        var total = await query.LongCountAsync(ct);

        // Lo más antiguo primero: la Dirección revisa en el orden en que se sincronizó.
        var rows = await query
            .OrderBy(p => p.LastSyncedAt ?? p.CreatedAt).ThenBy(p => p.PublicationId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Join(_db.People.AsNoTracking(), p => p.PersonId, person => person.PersonId,
                (p, person) => new PublicationReviewRow(
                    p.PublicationId, p.PersonId, person.FirstName, person.LastName, person.IdCard,
                    p.Title, p.JournalName, p.Doi, p.Link, p.PublicationDate, p.SourceProviderTypeId, p.ExternalId,
                    p.ApprovalStatus, p.LastSyncedAt, p.ApprovedAt, p.ReviewNotes))
            .ToListAsync(ct);

        return new PagedResult<PublicationReviewRow> { Items = rows, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<PublicationReviewCounts> GetCountsAsync(IReadOnlyCollection<int>? providerTypeIds, CancellationToken ct)
    {
        var scoped = _db.Publications.AsNoTracking()
            .Where(p => providerTypeIds == null || providerTypeIds.Contains(p.SourceProviderTypeId));

        var byStatus = await scoped.GroupBy(p => p.ApprovalStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var peoplePending = await scoped
            .Where(p => p.ApprovalStatus == PublicationApprovalStatus.SinRevision)
            .Select(p => p.PersonId).Distinct().CountAsync(ct);

        int Count(string status) => byStatus.FirstOrDefault(x => x.Status == status)?.Count ?? 0;
        return new PublicationReviewCounts(
            Count(PublicationApprovalStatus.SinRevision), Count(PublicationApprovalStatus.Aprobada),
            Count(PublicationApprovalStatus.Rechazada), peoplePending);
    }

    public Task<List<Publications>> GetByIdsTrackedAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        _db.Publications.Where(p => ids.Contains(p.PublicationId)).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    private IQueryable<Publications> ApplyFilter(
        IQueryable<Publications> query, PublicationReviewFilterDto filter, IReadOnlyCollection<int>? providerTypeIds)
    {
        var status = filter.Status?.Trim().ToUpperInvariant();
        if (PublicationApprovalStatus.IsValid(status))
            query = query.Where(p => p.ApprovalStatus == status);

        if (providerTypeIds is not null)
            query = query.Where(p => providerTypeIds.Contains(p.SourceProviderTypeId));

        if (filter.Year is int year)
            query = query.Where(p => p.PublicationDate != null && p.PublicationDate.Value.Year == year);

        if (filter.SyncedFrom is DateOnly from)
        {
            var fromDate = from.ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.LastSyncedAt != null && p.LastSyncedAt >= fromDate);
        }
        if (filter.SyncedTo is DateOnly to)
        {
            var toDate = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.LastSyncedAt != null && p.LastSyncedAt < toDate);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(p =>
                p.Title.Contains(term) ||
                (p.Doi != null && p.Doi.Contains(term)) ||
                (p.JournalName != null && p.JournalName.Contains(term)) ||
                _db.People.Any(person => person.PersonId == p.PersonId &&
                    (person.IdCard.Contains(term) || person.FirstName.Contains(term) || person.LastName.Contains(term))));
        }

        return query;
    }
}
