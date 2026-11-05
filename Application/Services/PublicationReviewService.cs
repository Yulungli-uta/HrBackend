using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Publications;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class PublicationReviewService : IPublicationReviewService
{
    private const int MaxIdsPerAction = 200;
    private const int MaxNotesLength = 300;

    private readonly IPublicationReviewRepository _repo;
    private readonly IScientificProviderCatalog _providers;
    private readonly IAppClock _clock;

    public PublicationReviewService(IPublicationReviewRepository repo, IScientificProviderCatalog providers, IAppClock clock)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<PagedResult<PublicationReviewItemDto>> GetPageAsync(PublicationReviewFilterDto filter, CancellationToken ct)
    {
        var providers = await _providers.GetAllAsync(ct);
        var typeIds = ResolveProviderScope(filter, providers);
        var page = await _repo.GetPageAsync(filter, typeIds, ct);

        var items = page.Items.Select(r =>
        {
            var provider = providers.FirstOrDefault(p => p.TypeId == r.SourceProviderTypeId);
            return new PublicationReviewItemDto(
                r.PublicationId, r.PersonId, $"{r.LastName} {r.FirstName}".Trim(), r.IdCard, r.Title, r.JournalName,
                r.Doi, r.Link ?? DoiNormalizer.ToUrl(r.Doi), r.PublicationDate, r.SourceProviderTypeId,
                provider?.Code ?? string.Empty, provider?.DisplayName ?? string.Empty, r.ExternalId,
                r.ApprovalStatus, r.LastSyncedAt, r.ApprovedAt, r.ReviewNotes);
        }).ToList();

        return new PagedResult<PublicationReviewItemDto> { Items = items, Page = page.Page, PageSize = page.PageSize, TotalCount = page.TotalCount };
    }

    public async Task<PublicationReviewSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var providers = await _providers.GetAllAsync(ct);
        var typeIds = providers.Where(p => p.RequiresApproval).Select(p => p.TypeId).ToList();
        var counts = await _repo.GetCountsAsync(typeIds, ct);
        return new PublicationReviewSummaryDto(counts.Pending, counts.Approved, counts.Rejected, counts.PeopleWithPending);
    }

    public Task<PublicationReviewResultDto> ApproveAsync(IReadOnlyList<int> ids, int? actorEmployeeId, CancellationToken ct) =>
        ApplyAsync(ids, PublicationApprovalStatus.SinRevision, PublicationApprovalStatus.Aprobada,
            "aprobar", actorEmployeeId, notes: null, ct);

    public Task<PublicationReviewResultDto> RejectAsync(IReadOnlyList<int> ids, string? notes, int? actorEmployeeId, CancellationToken ct) =>
        ApplyAsync(ids, PublicationApprovalStatus.SinRevision, PublicationApprovalStatus.Rechazada,
            "rechazar", actorEmployeeId, RequireNotes(notes, "rechazar"), ct);

    public Task<PublicationReviewResultDto> ReopenAsync(IReadOnlyList<int> ids, string? notes, int? actorEmployeeId, CancellationToken ct) =>
        ApplyAsync(ids, PublicationApprovalStatus.Aprobada, PublicationApprovalStatus.SinRevision,
            "reabrir", actorEmployeeId, RequireNotes(notes, "reabrir"), ct);

    public Task<PublicationReviewResultDto> RestoreAsync(IReadOnlyList<int> ids, int? actorEmployeeId, CancellationToken ct) =>
        ApplyAsync(ids, PublicationApprovalStatus.Rechazada, PublicationApprovalStatus.SinRevision,
            "restaurar", actorEmployeeId, notes: null, ct);

    private async Task<PublicationReviewResultDto> ApplyAsync(
        IReadOnlyList<int> ids, string fromStatus, string toStatus, string verb, int? actor, string? notes, CancellationToken ct)
    {
        var distinct = (ids ?? Array.Empty<int>()).Distinct().ToList();
        if (distinct.Count == 0) throw new BusinessRuleException("Seleccione al menos una publicación.");
        if (distinct.Count > MaxIdsPerAction)
            throw new BusinessRuleException($"Se pueden procesar hasta {MaxIdsPerAction} publicaciones por acción.");

        var found = await _repo.GetByIdsTrackedAsync(distinct, ct);
        var failures = new List<PublicationReviewFailureDto>();
        var now = _clock.Now;
        var processed = 0;

        foreach (var id in distinct)
        {
            var pub = found.FirstOrDefault(p => p.PublicationId == id);
            if (pub is null) { failures.Add(new(id, "La publicación no existe.")); continue; }
            if (pub.ApprovalStatus != fromStatus)
            {
                failures.Add(new(id, $"No se puede {verb}: está en estado {StatusLabel(pub.ApprovalStatus)}."));
                continue;
            }

            Transition(pub, toStatus, actor, now, notes);
            processed++;
        }

        if (processed > 0) await _repo.SaveChangesAsync(ct);
        return new PublicationReviewResultDto(processed, failures);
    }

    private static void Transition(Publications pub, string toStatus, int? actor, DateTime now, string? notes)
    {
        pub.ApprovalStatus = toStatus;
        // ApprovedAt/By guardan la última decisión de la Dirección (aprobar o rechazar); al devolverla a revisión se limpian.
        var decided = toStatus != PublicationApprovalStatus.SinRevision;
        pub.ApprovedAt = decided ? now : null;
        pub.ApprovedBy = decided ? actor : null;
        pub.ReviewNotes = notes;
    }

    private static string RequireNotes(string? notes, string verb)
    {
        var value = (notes ?? string.Empty).Trim();
        if (value.Length == 0) throw new BusinessRuleException($"Indique el motivo para {verb} la publicación.");
        if (value.Length > MaxNotesLength) throw new BusinessRuleException($"El motivo admite hasta {MaxNotesLength} caracteres.");
        return value;
    }

    private static string StatusLabel(string status) => status switch
    {
        PublicationApprovalStatus.Aprobada => "aprobada",
        PublicationApprovalStatus.Rechazada => "rechazada",
        _ => "en revisión",
    };

    private static IReadOnlyCollection<int>? ResolveProviderScope(PublicationReviewFilterDto filter, IReadOnlyList<DTOs.ScientificProviders.ScientificProviderInfo> providers)
    {
        if (!string.IsNullOrWhiteSpace(filter.ProviderCode))
        {
            var one = providers.FirstOrDefault(p => p.Code.Equals(filter.ProviderCode.Trim(), StringComparison.OrdinalIgnoreCase));
            return one is null ? new[] { -1 } : new[] { one.TypeId };
        }

        return filter.SyncedOnly ? providers.Where(p => p.RequiresApproval).Select(p => p.TypeId).ToList() : null;
    }
}
