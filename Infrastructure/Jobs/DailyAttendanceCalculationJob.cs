using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DailyAttendanceCalculationJob : BaseJob
{
    private readonly IAttendanceCalculationService _attendanceService;
    private readonly ILogger<DailyAttendanceCalculationJob> _logger;

    public DailyAttendanceCalculationJob(
        IAttendanceCalculationService attendanceService,
        ILogger<DailyAttendanceCalculationJob> logger)
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
            "Daily attendance pipeline targetDate={TargetDate:yyyy-MM-dd}",
            targetDate);

        await _attendanceService.ProcessAttendanceRunRangeAsync(
            targetDate,
            targetDate,
            cancellationToken);
    }
}