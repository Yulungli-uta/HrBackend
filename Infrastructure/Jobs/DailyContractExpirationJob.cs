using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job diario que detecta contratos VIGENTES cuya fecha de vencimiento ya pasó,
/// los marca como VENCIDO y deshabilita las cuentas AD cuando el tipo de contrato
/// tiene RequiresAdUserDisable = true.
/// Se ejecuta a las 2:00 AM para procesar los vencimientos del día anterior.
/// </summary>
[DisallowConcurrentExecution]
public sealed class DailyContractExpirationJob : BaseJob
{
    private readonly IContractExpirationService _expirationService;
    private readonly IEmployeeProvisioningClient _provisioningClient;
    private readonly ILogger<DailyContractExpirationJob> _logger;

    public DailyContractExpirationJob(
        IContractExpirationService expirationService,
        IEmployeeProvisioningClient provisioningClient,
        ILogger<DailyContractExpirationJob> logger)
        : base(logger)
    {
        _expirationService  = expirationService  ?? throw new ArgumentNullException(nameof(expirationService));
        _provisioningClient = provisioningClient ?? throw new ArgumentNullException(nameof(provisioningClient));
        _logger             = logger             ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        _logger.LogInformation(
            "Inicio proceso contratos vencidos. FechaLocal={Date:yyyy-MM-dd HH:mm}", now);

        var serviceToken = await _provisioningClient.GetServiceTokenAsync(cancellationToken)
                           ?? string.Empty;

        if (string.IsNullOrEmpty(serviceToken))
            _logger.LogWarning(
                "Token de servicio no disponible. Las cuentas AD no serán deshabilitadas " +
                "si RequiresAdUserDisable = true y RepositoryUta requiere autenticación.");

        var processed = await _expirationService.ProcessExpiredContractsAsync(serviceToken, cancellationToken);

        _logger.LogInformation(
            "Proceso contratos vencidos finalizado: {Processed} contratos procesados.", processed);
    }
}
