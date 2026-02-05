using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DailyNightMinutesCalculationJob : BaseJob
{
    private readonly IAttendanceCalculationService _attendanceService;
    private readonly ILogger<DailyNightMinutesCalculationJob> _logger;
    public DailyNightMinutesCalculationJob(
        IAttendanceCalculationService attendanceService,
        ILogger<DailyNightMinutesCalculationJob> logger)
        : base(logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var targetDate = now.Date.AddDays(-1);

        _logger.LogInformation(
            "Daily night minutes calculation targetDate={TargetDate:yyyy-MM-dd}",
            targetDate);

        await _attendanceService.CalculateNightMinutesAsync(
            targetDate,
            targetDate,
            null,
            cancellationToken);
    }
}
