using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job que ejecuta el cálculo de minutos nocturnos diariamente
/// Se ejecuta automáticamente cada día a las 3:00 AM (después del cálculo de asistencia)
/// Calcula los minutos nocturnos del día anterior
/// </summary>
[DisallowConcurrentExecution]
public class DailyNightMinutesCalculationJob : BaseJob
{
    private readonly IAttendanceCalculationService _attendanceService;
    private readonly ILogger<DailyNightMinutesCalculationJob> _logger;

    public DailyNightMinutesCalculationJob(
        IAttendanceCalculationService attendanceService,
        ILogger<DailyNightMinutesCalculationJob> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = GetCurrentDateTime(context);
            var yesterday = now.Date.AddDays(-1);

            _logger.LogInformation("Starting night minutes calculation for date: {Date}", yesterday);

            // Calcular minutos nocturnos del día anterior
            await _attendanceService.CalculateNightMinutesAsync(yesterday, yesterday, null, context.CancellationToken);

            _logger.LogInformation("Night minutes calculation completed successfully for date: {Date}", yesterday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing night minutes calculation job");
            throw;
        }
    }
}

