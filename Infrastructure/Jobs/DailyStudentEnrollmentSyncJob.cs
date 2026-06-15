using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Academic;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job diario que sincroniza las matrículas del sistema académico con el aprovisionamiento AD.
/// Se ejecuta al inicio de cada período académico (cron configurable).
/// Requiere PeriodCode y PreviousPeriod como JobData para operar.
/// </summary>
[DisallowConcurrentExecution]
public sealed class DailyStudentEnrollmentSyncJob : BaseJob
{
    private readonly IStudentEnrollmentSyncService _syncService;
    private readonly IEmployeeProvisioningClient _provisioningClient;
    private readonly ILogger<DailyStudentEnrollmentSyncJob> _logger;

    public DailyStudentEnrollmentSyncJob(
        IStudentEnrollmentSyncService syncService,
        IEmployeeProvisioningClient provisioningClient,
        ILogger<DailyStudentEnrollmentSyncJob> logger)
        : base(logger)
    {
        _syncService        = syncService        ?? throw new ArgumentNullException(nameof(syncService));
        _provisioningClient = provisioningClient ?? throw new ArgumentNullException(nameof(provisioningClient));
        _logger             = logger             ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var dataMap      = context.MergedJobDataMap;
        var periodCode   = dataMap.GetString("PeriodCode")   ?? string.Empty;
        var previousCode = dataMap.GetString("PreviousPeriod") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(periodCode))
        {
            _logger.LogWarning(
                "[STUDENT-JOB] PeriodCode no configurado en JobData. Sync omitido.");
            return;
        }

        _logger.LogInformation(
            "[STUDENT-JOB] Inicio sincronización. PeriodCode={Period} PreviousPeriod={Prev}",
            periodCode, previousCode);

        var serviceToken = await _provisioningClient.GetServiceTokenAsync(cancellationToken)
                           ?? string.Empty;

        if (string.IsNullOrEmpty(serviceToken))
            _logger.LogWarning(
                "[STUDENT-JOB] Token de servicio no disponible. Las cuentas AD no podrán crearse.");

        var provisioned = await _syncService.SyncPeriodAsync(periodCode, serviceToken, cancellationToken);

        _logger.LogInformation(
            "[STUDENT-JOB] Aprovisionamiento completado: {Count} cuenta(s) creada(s).", provisioned);

        if (!string.IsNullOrWhiteSpace(previousCode))
        {
            var disabled = await _syncService.DisableNonReEnrolledAsync(
                periodCode, previousCode, serviceToken, cancellationToken);

            _logger.LogInformation(
                "[STUDENT-JOB] Deshabilitación completada: {Count} cuenta(s) deshabilitada(s).", disabled);
        }
    }
}
