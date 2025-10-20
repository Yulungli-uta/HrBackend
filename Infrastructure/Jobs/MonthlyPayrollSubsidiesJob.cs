using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job que calcula los subsidios de nómina mensualmente
/// Se ejecuta automáticamente el día 1 de cada mes a las 4:00 AM
/// Calcula subsidios y recargos (nocturnos/feriados) del mes anterior
/// </summary>
[DisallowConcurrentExecution]
public class MonthlyPayrollSubsidiesJob : BaseJob
{
    private readonly IPayrollSubsidiesService _subsidiesService;
    private readonly ILogger<MonthlyPayrollSubsidiesJob> _logger;

    public MonthlyPayrollSubsidiesJob(
        IPayrollSubsidiesService subsidiesService,
        ILogger<MonthlyPayrollSubsidiesJob> logger)
    {
        _subsidiesService = subsidiesService;
        _logger = logger;
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = GetCurrentDateTime(context);
            var lastMonth = now.AddMonths(-1);
            var period = lastMonth.ToString("yyyy-MM"); // Formato: YYYY-MM

            _logger.LogInformation("Starting payroll subsidies calculation for period: {Period}", period);

            // Calcular subsidios del mes anterior
            await _subsidiesService.CalculateSubsidiesAsync(period, context.CancellationToken);

            _logger.LogInformation("Payroll subsidies calculation completed successfully for period: {Period}", period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payroll subsidies calculation job for period");
            throw;
        }
    }
}

