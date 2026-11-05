using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.PublicationSync;
using WsUtaSystem.Application.DTOs.ResearcherProfiles;
using WsUtaSystem.Application.DTOs.ScientificProviders;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class PublicationSyncService : IPublicationSyncService
{
    private const string PreviewCachePrefix = "PubSyncPreview:";
    private static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(10);
    private const int MaxSummaryLength = 500;

    // Equivalencias con el catálogo PUBLICATION_TYPE de HR (hoy: "Ponencia en Evento" y "EDITORIAL").
    private const string PublicationTypeCategory = "PUBLICATION_TYPE";
    private const string ConferenceTypeName = "Ponencia en Evento";
    private const string DefaultTypeName = "EDITORIAL";
    private static readonly HashSet<string> ConferenceSubtypes = new(StringComparer.OrdinalIgnoreCase) { "cp", "cr" };

    private readonly IScientificProviderCatalog _catalog;
    private readonly IResearcherProfilesService _profiles;
    private readonly IPublicationSyncRepository _repo;
    private readonly IRefTypesRepository _refTypes;
    private readonly IReadOnlyList<IPublicationSourceProvider> _adapters;
    private readonly IMemoryCache _cache;
    private readonly IAppClock _clock;
    private readonly ILogger<PublicationSyncService> _logger;

    public PublicationSyncService(
        IScientificProviderCatalog catalog,
        IResearcherProfilesService profiles,
        IPublicationSyncRepository repo,
        IRefTypesRepository refTypes,
        IEnumerable<IPublicationSourceProvider> adapters,
        IMemoryCache cache,
        IAppClock clock,
        ILogger<PublicationSyncService> logger)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _refTypes = refTypes ?? throw new ArgumentNullException(nameof(refTypes));
        _adapters = (adapters ?? throw new ArgumentNullException(nameof(adapters))).ToList();
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─── Estado para el diálogo ────────────────────────────────────────────────

    public async Task<PublicationSyncReadinessDto> GetReadinessAsync(int personId, bool canEdit, CancellationToken ct)
    {
        var profile = await _profiles.GetEntityAsync(personId, ct);
        var providers = (await _catalog.GetAllAsync(ct)).Where(p => p.IsActive && !p.IsSystem).ToList();
        var result = new List<SyncProviderStatusDto>(providers.Count);

        foreach (var provider in providers)
        {
            var ids = profile?.ProviderIds
                .Where(x => x.IsActive && x.ProviderTypeId == provider.TypeId)
                .OrderBy(x => x.ExternalId).ToList() ?? new List<ResearcherProviderId>();

            var missing = new List<string>();
            if (profile?.Orcid is null) missing.Add(PublicationSyncMissing.Orcid);
            if (provider.RequiresProviderId && ids.Count == 0) missing.Add(PublicationSyncMissing.ProviderId);
            var adapter = FindAdapter(provider.Code);
            if (adapter is null) missing.Add(PublicationSyncMissing.NoAdapter);
            else if (!await adapter.IsConfiguredAsync(ct)) missing.Add(PublicationSyncMissing.NotConfigured);

            var last = ids.Where(x => x.LastSyncedAt is not null).OrderByDescending(x => x.LastSyncedAt).FirstOrDefault();
            result.Add(new SyncProviderStatusDto(
                provider.Code, provider.DisplayName, provider.IdLabel, provider.ProfileUrlTemplate, provider.IdPattern,
                provider.HasAdapter, provider.RequiresProviderId, provider.UsesProfileOrcid, provider.AllowsMultipleIds,
                ids.Select(x => new ResearcherProviderIdDto(x.ResearcherProviderIdId, x.ProviderTypeId, provider.Code, x.ExternalId,
                    x.VerifiedAt, x.LastSyncedAt, x.LastSyncStatus, x.LastSyncSummary)).ToList(),
                missing.Count == 0, missing, last?.LastSyncedAt, last?.LastSyncStatus, last?.LastSyncSummary));
        }

        return new PublicationSyncReadinessDto(personId, profile?.Orcid is not null, profile?.Orcid, canEdit, result);
    }

    // ─── Vista previa ──────────────────────────────────────────────────────────

    public async Task<PublicationSyncPreviewDto> PreviewAsync(int personId, string providerCode, CancellationToken ct)
    {
        var (provider, adapter, profile, ids) = await ResolveSyncContextAsync(personId, providerCode, ct);
        var fetched = await adapter.FetchPublicationsAsync(new ExternalAuthorRef(profile.Orcid!, ids), ct);
        var lastSynced = profile.ProviderIds.Where(x => x.IsActive && x.ProviderTypeId == provider.TypeId)
            .Max(x => x.LastSyncedAt);

        if (!fetched.Available)
        {
            return new PublicationSyncPreviewDto(Guid.Empty, personId, provider.Code, false, false, fetched.Message,
                0, 0, 0, 0, 0, 0, 0, lastSynced, Array.Empty<PublicationSyncItemDto>());
        }

        var items = Deduplicate(fetched.Items);
        var existing = await _repo.GetPersonPublicationsAsync(personId, tracked: false, ct);
        var types = await LoadPublicationTypesAsync(ct);

        var dtos = new List<PublicationSyncItemDto>(items.Count);
        foreach (var ext in items)
        {
            if (!IsUsable(ext))
            {
                dtos.Add(ToItem(ext, PublicationSyncItemStatus.Error, null, Array.Empty<Publications>(), "El proveedor devolvió datos incompletos."));
                continue;
            }
            var outcome = PublicationMatcher.Match(ext, provider.TypeId, ResolveTypeId(ext, types), existing);
            dtos.Add(ToItem(ext, outcome.Status, outcome.Matched, outcome.Candidates, null));
        }

        var token = Guid.NewGuid();
        _cache.Set(PreviewCachePrefix + token, new PreviewState(personId, provider.Code, items, fetched.IsPartial), PreviewLifetime);

        int Count(string status) => dtos.Count(d => d.Status == status);
        return new PublicationSyncPreviewDto(
            token, personId, provider.Code, true, fetched.IsPartial, fetched.Message, dtos.Count,
            Count(PublicationSyncItemStatus.New), Count(PublicationSyncItemStatus.Existing), Count(PublicationSyncItemStatus.Completable),
            Count(PublicationSyncItemStatus.Locked), Count(PublicationSyncItemStatus.Ambiguous), Count(PublicationSyncItemStatus.Error),
            lastSynced, dtos, fetched.SkippedByType);
    }

    // ─── Confirmación ──────────────────────────────────────────────────────────

    public async Task<PublicationSyncResultDto> ConfirmAsync(int personId, PublicationSyncConfirmRequest request, CancellationToken ct)
    {
        if (!_cache.TryGetValue(PreviewCachePrefix + request.PreviewToken, out PreviewState? state) || state is null)
            throw new BusinessRuleException("La vista previa venció. Vuelva a consultar antes de confirmar.");
        if (state.PersonId != personId || !state.ProviderCode.Equals(request.ProviderCode?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("La vista previa no corresponde a esta persona o proveedor. Vuelva a consultar.");

        var (provider, _, profile, _) = await ResolveSyncContextAsync(personId, state.ProviderCode, ct);
        var resolutions = (request.Resolutions ?? Array.Empty<SyncResolutionDto>())
            .GroupBy(r => r.ExternalId).ToDictionary(g => g.Key, g => g.Last());

        var result = await ApplyAsync(personId, provider, profile, state.Items, state.IsPartial, resolutions, automatic: false, ct);

        // Un solo uso: la vista previa se descarta al confirmar.
        _cache.Remove(PreviewCachePrefix + request.PreviewToken);
        return result;
    }

    public async Task<AutomaticSyncOutcome> SyncAutomaticallyAsync(int personId, string providerCode, CancellationToken ct)
    {
        var (provider, adapter, profile, ids) = await ResolveSyncContextAsync(personId, providerCode, ct);
        var fetched = await adapter.FetchPublicationsAsync(new ExternalAuthorRef(profile.Orcid!, ids), ct);
        if (!fetched.Available)
        {
            await RecordFailureAsync(profile, provider, fetched.Message, ct);
            return new AutomaticSyncOutcome(false, false, 0, 0, 0, fetched.QuotaRemaining, fetched.ErrorKind, fetched.Message);
        }

        var result = await ApplyAsync(personId, provider, profile, Deduplicate(fetched.Items), fetched.IsPartial,
            resolutions: null, automatic: true, ct);
        return new AutomaticSyncOutcome(true, fetched.IsPartial, result.Created, result.Completed, result.Ambiguous,
            fetched.QuotaRemaining, null, fetched.Message);
    }

    // ─── Núcleo: aplica en una transacción con candado ─────────────────────────

    private async Task<PublicationSyncResultDto> ApplyAsync(
        int personId, ScientificProviderInfo provider, ResearcherProfile profile, IReadOnlyList<ExternalPublication> items,
        bool isPartial, IReadOnlyDictionary<string, SyncResolutionDto>? resolutions, bool automatic, CancellationToken ct)
    {
        try
        {
            var outcomeDto = await _repo.RunExclusiveAsync($"PUBSYNC:{personId}:{provider.Code}", async () =>
            {
            // Se recarga lo registrado dentro del candado: lo que se aplica es lo vigente, no lo que se vio en la vista previa.
            var existing = await _repo.GetPersonPublicationsAsync(personId, tracked: true, ct);
            var types = await LoadPublicationTypesAsync(ct);
            var now = _clock.Now;
            int created = 0, completed = 0, skipped = 0, locked = 0, ambiguous = 0;
            var unresolved = new List<string>();

            // Fase 1: decidir qué hacer con cada artículo SIN tocar nada (así un error de validación no deja datos a medias).
            var matchSet = new List<Publications>(existing);
            var plan = new List<Action>();

            foreach (var ext in items)
            {
                if (!IsUsable(ext)) { skipped++; continue; }

                var typeId = ResolveTypeId(ext, types);
                var outcome = PublicationMatcher.Match(ext, provider.TypeId, typeId, matchSet);

                switch (outcome.Status)
                {
                    case PublicationSyncItemStatus.New:
                        PlanNew(ext, personId, provider, typeId, now, matchSet, plan);
                        created++;
                        break;

                    case PublicationSyncItemStatus.Existing:
                        plan.Add(() => Touch(outcome.Matched!, provider, ext, now));
                        break;

                    case PublicationSyncItemStatus.Completable:
                        plan.Add(() => Complete(outcome.Matched!, ext, provider, typeId, now));
                        completed++;
                        break;

                    case PublicationSyncItemStatus.Locked:
                        locked++;
                        break;

                    case PublicationSyncItemStatus.Ambiguous when automatic:
                        ambiguous++;
                        break;

                    case PublicationSyncItemStatus.Ambiguous:
                        if (resolutions is null || !resolutions.TryGetValue(ext.ExternalId, out var resolution))
                        {
                            unresolved.Add(ext.ExternalId);
                            break;
                        }
                        switch (resolution.Action?.ToUpperInvariant())
                        {
                            case PublicationSyncResolutionAction.Link:
                                var target = outcome.Candidates.FirstOrDefault(c => c.PublicationId == resolution.PublicationId)
                                    ?? throw new BusinessRuleException("La publicación elegida no es una de las coincidencias propuestas.");
                                if (PublicationRules.IsLocked(target.ApprovalStatus)) { locked++; break; }
                                plan.Add(() => Complete(target, ext, provider, typeId, now));
                                completed++;
                                break;
                            case PublicationSyncResolutionAction.Import:
                                PlanNew(ext, personId, provider, typeId, now, matchSet, plan);
                                created++;
                                break;
                            default:
                                skipped++;
                                break;
                        }
                        break;
                }
            }

            if (unresolved.Count > 0)
                throw new BusinessRuleException($"Resuelva las {unresolved.Count} publicaciones ambiguas antes de confirmar.");

            // Fase 2: aplicar todo en la misma transacción.
            foreach (var step in plan) step();
            RecordOutcome(profile, provider, now, created, completed, skipped + ambiguous, locked, isPartial, markVerified: !automatic);

            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Otra sincronización creó una de estas publicaciones entre la lectura y la escritura.
                _logger.LogWarning(ex, "Violación de índice único al sincronizar publicaciones de PersonId={PersonId}.", personId);
                throw new BusinessRuleException("Otra sincronización registró publicaciones al mismo tiempo. Vuelva a consultar y confirmar.");
            }

            _logger.LogInformation(
                "Sincronización de publicaciones PersonId={PersonId} Proveedor={Provider} Modo={Mode}: {Created} nuevas, {Completed} completadas, {Locked} congeladas, {Ambiguous} ambiguas, {Skipped} omitidas, parcial={Partial}",
                personId, provider.Code, automatic ? "AUTOMATICO" : "MANUAL", created, completed, locked, ambiguous, skipped, isPartial);

            return new PublicationSyncResultDto(created, completed, skipped, locked, ambiguous, isPartial);
            }, ct);

            return outcomeDto
                ?? throw new BusinessRuleException("Ya hay una sincronización en curso para esta persona. Intente de nuevo en unos minutos.");
        }
        finally
        {
            // El contexto puede reutilizarse (la corrida masiva usa uno solo): un fallo no debe dejar entidades pendientes
            // que se intentarían guardar con la siguiente persona.
            _repo.ClearTracking();
        }
    }

    /// <summary>Prepara una publicación nueva; se agrega al contexto recién en la fase de aplicación.</summary>
    private void PlanNew(
        ExternalPublication ext, int personId, ScientificProviderInfo provider, int? typeId, DateTime now,
        List<Publications> matchSet, List<Action> plan)
    {
        var entity = PublicationMapper.ToEntity(ext, personId, provider.TypeId, typeId, now);
        plan.Add(() => _repo.Add(entity));
        matchSet.Add(entity); // las comparaciones siguientes del mismo lote ven lo ya planificado
    }

    /// <summary>Deja constancia del fallo en el ID de la persona; si no se puede guardar, no interrumpe la corrida.</summary>
    private async Task RecordFailureAsync(ResearcherProfile profile, ScientificProviderInfo provider, string? message, CancellationToken ct)
    {
        try
        {
            var now = _clock.Now;
            var text = string.IsNullOrWhiteSpace(message) ? "El proveedor no respondió." : message;
            foreach (var id in profile.ProviderIds.Where(x => x.IsActive && x.ProviderTypeId == provider.TypeId))
            {
                id.LastSyncedAt = now;
                id.LastSyncStatus = "ERROR";
                id.LastSyncSummary = text.Length > MaxSummaryLength ? text[..MaxSummaryLength] : text;
            }
            await _repo.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "No se pudo registrar el fallo de sincronización de PersonId={PersonId}.", profile.PersonId);
        }
    }

    private static void Complete(Publications row, ExternalPublication ext, ScientificProviderInfo provider, int? typeId, DateTime now)
    {
        var mapped = PublicationMapper.ToEntity(ext, row.PersonId, provider.TypeId, typeId, now);
        PublicationMapper.CompleteEmptyFields(row, mapped, apply: true);
        Touch(row, provider, ext, now);
    }

    /// <summary>Marca la fecha de sincronización solo en lo que ya viene de este proveedor; histórico y manual no se alteran.</summary>
    private static void Touch(Publications row, ScientificProviderInfo provider, ExternalPublication ext, DateTime now)
    {
        if (row.SourceProviderTypeId == provider.TypeId &&
            string.Equals(row.ExternalId, ext.ExternalId, StringComparison.OrdinalIgnoreCase))
            row.LastSyncedAt = now;
    }

    private static void RecordOutcome(
        ResearcherProfile profile, ScientificProviderInfo provider, DateTime now,
        int created, int completed, int notApplied, int locked, bool isPartial, bool markVerified)
    {
        var summary = $"{created} nuevas, {completed} completadas, {locked} congeladas, {notApplied} sin aplicar" +
                      (isPartial ? " (resultado parcial: faltaron páginas del proveedor)" : string.Empty);
        if (summary.Length > MaxSummaryLength) summary = summary[..MaxSummaryLength];

        foreach (var id in profile.ProviderIds.Where(x => x.IsActive && x.ProviderTypeId == provider.TypeId))
        {
            if (markVerified && id.VerifiedAt is null) id.VerifiedAt = now;
            id.LastSyncedAt = now;
            id.LastSyncStatus = isPartial ? "PARCIAL" : "OK";
            id.LastSyncSummary = summary;
        }
    }

    // ─── Contexto y utilidades ─────────────────────────────────────────────────

    private async Task<(ScientificProviderInfo Provider, IPublicationSourceProvider Adapter, ResearcherProfile Profile, IReadOnlyList<string> Ids)>
        ResolveSyncContextAsync(int personId, string providerCode, CancellationToken ct)
    {
        var provider = await _catalog.GetRequiredAsync(providerCode, ct);
        if (!provider.IsActive || provider.IsSystem)
            throw new BusinessRuleException($"El proveedor {provider.DisplayName} no está disponible para sincronizar.");

        var adapter = FindAdapter(provider.Code)
            ?? throw new BusinessRuleException($"La sincronización con {provider.DisplayName} todavía no está disponible.");
        if (!await adapter.IsConfiguredAsync(ct))
            throw new BusinessRuleException($"La sincronización con {provider.DisplayName} no está configurada. Pida a un administrador cargar la URL y las credenciales en Parámetros del Sistema.");

        var profile = await _profiles.GetEntityAsync(personId, ct)
            ?? throw new BusinessRuleException("Falta el ORCID de la persona: debe registrarlo antes de sincronizar.");
        if (string.IsNullOrWhiteSpace(profile.Orcid))
            throw new BusinessRuleException("Falta el ORCID de la persona: debe registrarlo antes de sincronizar.");

        var ids = profile.ProviderIds.Where(x => x.IsActive && x.ProviderTypeId == provider.TypeId)
            .Select(x => x.ExternalId).ToList();
        if (provider.RequiresProviderId && ids.Count == 0)
            throw new BusinessRuleException($"Falta el {provider.IdLabel ?? "ID"} de {provider.DisplayName}: debe registrarlo antes de sincronizar.");

        return (provider, adapter, profile, ids);
    }

    private IPublicationSourceProvider? FindAdapter(string code) =>
        _adapters.FirstOrDefault(a => a.ProviderCode.Equals(code, StringComparison.OrdinalIgnoreCase));

    private static bool IsUsable(ExternalPublication ext) =>
        !string.IsNullOrWhiteSpace(ext.ExternalId) && !string.IsNullOrWhiteSpace(ext.Title);

    private static IReadOnlyList<ExternalPublication> Deduplicate(IEnumerable<ExternalPublication> items) =>
        items.GroupBy(i => i.ExternalId, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();

    /// <summary>TypeID de los tipos de publicación de HR, por nombre (el TypeID varía entre ambientes).</summary>
    private async Task<Dictionary<string, int>> LoadPublicationTypesAsync(CancellationToken ct) =>
        (await _refTypes.GetByCategoryAsync(PublicationTypeCategory, ct))
            .Where(t => t.IsActive)
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().TypeId, StringComparer.OrdinalIgnoreCase);

    private static int? ResolveTypeId(ExternalPublication ext, IReadOnlyDictionary<string, int> types)
    {
        var name = ext.SubtypeCode is not null && ConferenceSubtypes.Contains(ext.SubtypeCode) ? ConferenceTypeName : DefaultTypeName;
        return types.TryGetValue(name, out var id) ? id : null;
    }

    private static PublicationSyncItemDto ToItem(
        ExternalPublication ext, string status, Publications? matched, IReadOnlyList<Publications> candidates, string? message) =>
        new(ext.ExternalId, ext.Title, ext.JournalName, DoiNormalizer.Normalize(ext.Doi), ext.PublicationDate,
            SubtypeLabel(ext.SubtypeCode), ext.AffiliationNames, ext.RecordUrl, status, matched?.PublicationId,
            candidates.Select(c => new SyncCandidateDto(c.PublicationId, c.Title, c.Doi, c.PublicationDate, c.ApprovalStatus)).ToList(),
            message);

    private static string? SubtypeLabel(string? code) => code?.ToLowerInvariant() switch
    {
        "ar" => "Artículo",
        "re" => "Revisión",
        "cp" => "Ponencia",
        "cr" => "Revisión de congreso",
        "ch" => "Capítulo de libro",
        "bk" => "Libro",
        "le" => "Carta",
        "ed" => "Editorial",
        "no" => "Nota",
        "er" => "Fe de erratas",
        "sh" => "Artículo breve",
        "ip" => "Artículo en prensa",
        null or "" => null,
        _ => code,
    };

    private sealed record PreviewState(int PersonId, string ProviderCode, IReadOnlyList<ExternalPublication> Items, bool IsPartial);
}
