using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job que ejecuta el cálculo de asistencia diariamente
/// Se ejecuta automáticamente cada día a las 2:00 AM
/// Calcula la asistencia del día anterior
/// </summary>
[DisallowConcurrentExecution]
public class DailyAttendanceCalculationJob : BaseJob
{
    private readonly IAttendanceCalculationService _attendanceService;
    private readonly ILogger<DailyAttendanceCalculationJob> _logger;

    public DailyAttendanceCalculationJob(
        IAttendanceCalculationService attendanceService,
        ILogger<DailyAttendanceCalculationJob> logger)
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

            _logger.LogInformation("Starting daily attendance calculation for date: {Date}", yesterday);

            // Calcular asistencia del día anterior
            await _attendanceService.CalculateRangeAsync(yesterday, yesterday, null, context.CancellationToken);

            _logger.LogInformation("Daily attendance calculation completed successfully for date: {Date}", yesterday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing daily attendance calculation job");
            throw;
        }
    }
}

