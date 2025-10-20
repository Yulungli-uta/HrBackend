using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job que calcula los descuentos de nómina mensualmente
/// Se ejecuta automáticamente el día 1 de cada mes a las 3:00 AM
/// Calcula descuentos por atrasos y ausencias del mes anterior
/// </summary>
[DisallowConcurrentExecution]
public class MonthlyPayrollDiscountsJob : BaseJob
{
    private readonly IPayrollDiscountsService _discountsService;
    private readonly ILogger<MonthlyPayrollDiscountsJob> _logger;

    public MonthlyPayrollDiscountsJob(
        IPayrollDiscountsService discountsService,
        ILogger<MonthlyPayrollDiscountsJob> logger)
    {
        _discountsService = discountsService;
        _logger = logger;
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = GetCurrentDateTime(context);
            var lastMonth = now.AddMonths(-1);
            var period = lastMonth.ToString("yyyy-MM"); // Formato: YYYY-MM

            _logger.LogInformation("Starting payroll discounts calculation for period: {Period}", period);

            // Calcular descuentos del mes anterior
            await _discountsService.CalculateDiscountsAsync(period, context.CancellationToken);

            _logger.LogInformation("Payroll discounts calculation completed successfully for period: {Period}", period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payroll discounts calculation job for period");
            throw;
        }
    }
}

