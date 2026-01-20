using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class MonthlyPayrollSubsidiesJob : BaseJob
{
    private readonly IPayrollSubsidiesService _subsidiesService;

    public MonthlyPayrollSubsidiesJob(
        IPayrollSubsidiesService subsidiesService,
        ILogger<MonthlyPayrollSubsidiesJob> logger)
        : base(logger)
    {
        _subsidiesService = subsidiesService;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var period = now.AddMonths(-1).ToString("yyyy-MM");

        Logger.LogInformation(
            "Monthly payroll subsidies calculation period={Period}",
            period);

        await _subsidiesService.CalculateSubsidiesAsync(period, cancellationToken);
    }
}
