using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job que calcula el precio de horas extra mensualmente
/// Se ejecuta automáticamente el día 1 de cada mes a las 2:00 AM
/// Calcula las horas extra del mes anterior
/// </summary>
[DisallowConcurrentExecution]
public class MonthlyOvertimePriceJob : BaseJob
{
    private readonly IOvertimePriceService _overtimeService;
    private readonly ILogger<MonthlyOvertimePriceJob> _logger;

    public MonthlyOvertimePriceJob(
        IOvertimePriceService overtimeService,
        ILogger<MonthlyOvertimePriceJob> logger)
    {
        _overtimeService = overtimeService;
        _logger = logger;
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = GetCurrentDateTime(context);
            var lastMonth = now.AddMonths(-1);
            var period = lastMonth.ToString("yyyy-MM"); // Formato: YYYY-MM

            _logger.LogInformation("Starting overtime price calculation for period: {Period}", period);

            // Calcular precio de horas extra del mes anterior
            await _overtimeService.CalculateOvertimePriceAsync(period, context.CancellationToken);

            _logger.LogInformation("Overtime price calculation completed successfully for period: {Period}", period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing overtime price calculation job for period");
            throw;
        }
    }
}

