using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class MonthlyOvertimePriceJob : BaseJob
{
    private readonly IOvertimePriceService _overtimeService;

    public MonthlyOvertimePriceJob(
        IOvertimePriceService overtimeService,
        ILogger<MonthlyOvertimePriceJob> logger)
        : base(logger)
    {
        _overtimeService = overtimeService;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var period = now.AddMonths(-1).ToString("yyyy-MM"); // mes anterior

        Logger.LogInformation(
            "Monthly overtime price calculation period={Period}",
            period);

        await _overtimeService.CalculateOvertimePriceAsync(period, cancellationToken);
    }
}
