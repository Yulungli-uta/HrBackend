using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DailyAccrueVacationBalance : BaseJob
{
    private readonly ITimeBalancesService _timeService;

    public DailyAccrueVacationBalance(
        ITimeBalancesService timeService,
        ILogger<DailyAccrueVacationBalance> logger)
        : base(logger)
    {
        _timeService = timeService;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var targetDate = now.Date.AddDays(-1);

        Logger.LogInformation(
            "Daily accrue vacation balance targetDate={TargetDate:yyyy-MM-dd}",
            targetDate);

        await _timeService.CalculateAccrueVacationBalance(
            targetDate,
            targetDate,
            null,
            cancellationToken);
    }
}
