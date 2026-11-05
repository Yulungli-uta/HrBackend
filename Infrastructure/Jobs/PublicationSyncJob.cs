using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Sincronización masiva de publicaciones científicas (Scopus…): recorre a los docentes que ya registraron su ORCID y su ID
/// en el proveedor, importa lo nuevo (queda SIN_REVISION) y completa vacíos. Registrado "durable" y sin horario fijo: se
/// dispara manualmente desde /scheduled-jobs/publication-sync/run hasta que se decida una periodicidad.
/// </summary>
[DisallowConcurrentExecution]
public sealed class PublicationSyncJob : BaseJob
{
    private readonly IPublicationMassSyncService _massSync;
    private readonly ILogger<PublicationSyncJob> _logger;

    public PublicationSyncJob(
        IPublicationMassSyncService massSync,
        ILogger<PublicationSyncJob> logger,
        IJobExecutionLogService jobExecutionLogService)
        : base(logger, jobExecutionLogService)
    {
        _massSync = massSync ?? throw new ArgumentNullException(nameof(massSync));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteJobAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PUBLICATION-SYNC-JOB] Inicio de la sincronización masiva de publicaciones.");

        var result = await _massSync.RunAsync(cancellationToken);

        _logger.LogInformation(
            "[PUBLICATION-SYNC-JOB] Terminó: {Processed} persona(s) procesada(s), {WithError} con error, {Created} nueva(s), " +
            "{Completed} completada(s), {Ambiguous} ambigua(s) omitida(s). Detenida antes de tiempo: {Stopped}. {Reason}",
            result.PeopleProcessed, result.PeopleWithError, result.Created, result.Completed, result.AmbiguousSkipped,
            result.StoppedEarly, result.StopReason);
    }
}
