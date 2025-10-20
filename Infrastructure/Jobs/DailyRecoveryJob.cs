using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job que aplica recuperaciones de tiempo diariamente
/// Se ejecuta automáticamente cada día a las 5:00 AM
/// Aplica recuperaciones del día anterior
/// </summary>
[DisallowConcurrentExecution]
public class DailyRecoveryJob : BaseJob
{
    private readonly IRecoveryService _recoveryService;
    private readonly ILogger<DailyRecoveryJob> _logger;

    public DailyRecoveryJob(
        IRecoveryService recoveryService,
        ILogger<DailyRecoveryJob> logger)
    {
        _recoveryService = recoveryService;
        _logger = logger;
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = GetCurrentDateTime(context);
            var yesterday = now.Date.AddDays(-1);

            _logger.LogInformation("Starting recovery application for date: {Date}", yesterday);

            // Aplicar recuperaciones del día anterior
            await _recoveryService.ApplyRecoveryAsync(yesterday, yesterday, null, context.CancellationToken);

            _logger.LogInformation("Recovery applied successfully for date: {Date}", yesterday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing recovery job");
            throw;
        }
    }
}

