using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job que aplica justificaciones aprobadas diariamente
/// Se ejecuta automáticamente cada día a las 4:00 AM
/// Aplica justificaciones del día anterior
/// </summary>
[DisallowConcurrentExecution]
public class DailyJustificationsJob : BaseJob
{
    private readonly IJustificationsService _justificationsService;
    private readonly ILogger<DailyJustificationsJob> _logger;

    public DailyJustificationsJob(
        IJustificationsService justificationsService,
        ILogger<DailyJustificationsJob> logger)
    {
        _justificationsService = justificationsService;
        _logger = logger;
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = GetCurrentDateTime(context);
            var yesterday = now.Date.AddDays(-1);

            _logger.LogInformation("Starting justifications application for date: {Date}", yesterday);

            // Aplicar justificaciones del día anterior
            await _justificationsService.ApplyJustificationsAsync(yesterday, yesterday, null, context.CancellationToken);

            _logger.LogInformation("Justifications applied successfully for date: {Date}", yesterday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing justifications job");
            throw;
        }
    }
}

