using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.PublicationSync;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;

namespace WsUtaSystem.Application.Services;

public sealed class PublicationMassSyncService : IPublicationMassSyncService
{
    /// <summary>Por debajo de este cupo semanal restante la corrida se detiene para no dejar sin consultas a la sincronización individual.</summary>
    private const int MinQuotaToContinue = 300;

    /// <summary>Pausa entre personas: respeta el límite por segundo del proveedor.</summary>
    private static readonly TimeSpan DelayBetweenPeople = TimeSpan.FromMilliseconds(250);

    private readonly IScientificProviderCatalog _catalog;
    private readonly IPublicationSyncRepository _repo;
    private readonly IPublicationSyncService _sync;
    private readonly IReadOnlyList<IPublicationSourceProvider> _adapters;
    private readonly ILogger<PublicationMassSyncService> _logger;

    public PublicationMassSyncService(
        IScientificProviderCatalog catalog,
        IPublicationSyncRepository repo,
        IPublicationSyncService sync,
        IEnumerable<IPublicationSourceProvider> adapters,
        ILogger<PublicationMassSyncService> logger)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _sync = sync ?? throw new ArgumentNullException(nameof(sync));
        _adapters = (adapters ?? throw new ArgumentNullException(nameof(adapters))).ToList();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PublicationMassSyncResultDto> RunAsync(CancellationToken ct)
    {
        var providers = (await _catalog.GetAllAsync(ct))
            .Where(p => p.IsActive && !p.IsSystem && p.HasAdapter)
            .ToList();

        int processed = 0, withError = 0, created = 0, completed = 0, ambiguous = 0;
        var stoppedEarly = false;
        string? stopReason = null;

        foreach (var provider in providers)
        {
            var adapter = _adapters.FirstOrDefault(a => a.ProviderCode.Equals(provider.Code, StringComparison.OrdinalIgnoreCase));
            if (adapter is null || !await adapter.IsConfiguredAsync(ct))
            {
                _logger.LogWarning("Sincronización masiva: el proveedor {Provider} no está configurado; se omite.", provider.Code);
                continue;
            }

            var targets = await _repo.GetMassTargetsAsync(provider.TypeId, provider.RequiresProviderId, ct);
            _logger.LogInformation("Sincronización masiva {Provider}: {Count} persona(s) con ORCID (e ID si el proveedor lo exige).", provider.Code, targets.Count);

            foreach (var target in targets)
            {
                ct.ThrowIfCancellationRequested();
                AutomaticSyncOutcome outcome;
                try
                {
                    outcome = await _sync.SyncAutomaticallyAsync(target.PersonId, provider.Code, ct);
                }
                catch (BusinessRuleException ex)
                {
                    // Por ejemplo, la persona ya tiene una sincronización en curso: se omite y se sigue con las demás.
                    _logger.LogWarning("Sincronización masiva: se omite PersonId={PersonId}: {Message}", target.PersonId, ex.Message);
                    withError++;
                    continue;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Sincronización masiva: falló PersonId={PersonId}; se continúa con las demás.", target.PersonId);
                    withError++;
                    continue;
                }

                if (!outcome.Available)
                {
                    withError++;
                    if (ExternalErrorKinds.StopsBatch(outcome.ErrorKind))
                    {
                        stoppedEarly = true;
                        stopReason = outcome.Message;
                        break;
                    }
                    continue;
                }

                processed++;
                created += outcome.Created;
                completed += outcome.Completed;
                ambiguous += outcome.AmbiguousSkipped;

                if (outcome.QuotaRemaining is int remaining && remaining < MinQuotaToContinue)
                {
                    stoppedEarly = true;
                    stopReason = $"Quedan {remaining} consultas del cupo semanal de {provider.DisplayName}; se detiene para no agotarlo.";
                    break;
                }

                await Task.Delay(DelayBetweenPeople, ct);
            }

            if (stoppedEarly) break;
        }

        return new PublicationMassSyncResultDto(processed, withError, created, completed, ambiguous, stoppedEarly, stopReason);
    }
}
