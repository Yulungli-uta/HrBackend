using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class MonthlyPayrollDiscountsJob : BaseJob
{
    private readonly IPayrollDiscountsService _discountsService;

    public MonthlyPayrollDiscountsJob(
        IPayrollDiscountsService discountsService,
        ILogger<MonthlyPayrollDiscountsJob> logger)
        : base(logger)
    {
        _discountsService = discountsService;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var period = now.AddMonths(-1).ToString("yyyy-MM");

        Logger.LogInformation(
            "Monthly payroll discounts calculation period={Period}",
            period);

        await _discountsService.CalculateDiscountsAsync(period, cancellationToken);
    }
}
