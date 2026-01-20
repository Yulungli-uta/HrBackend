using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DailyAttendanceCalculationJob : BaseJob
{
    private readonly IAttendanceCalculationService _attendanceService;

    public DailyAttendanceCalculationJob(
        IAttendanceCalculationService attendanceService,
        ILogger<DailyAttendanceCalculationJob> logger)
        : base(logger)
    {
        _attendanceService = attendanceService;
    }

    protected override async Task ExecuteJobAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var targetDate = now.Date.AddDays(-1);

        // Nota: el cron real está a la 01:00 (QuartzConfiguration), no 02:00. :contentReference[oaicite:1]{index=1}
        Logger.LogInformation("Daily attendance calculation targetDate={TargetDate:yyyy-MM-dd}", targetDate);

        await _attendanceService.ProcessAttendanceRange(targetDate, targetDate, cancellationToken);
    }
}
