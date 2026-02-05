using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DailyRecoveryJob : BaseJob
{
    private readonly IRecoveryService _recoveryService;
    private readonly ILogger<DailyRecoveryJob> _logger;

    public DailyRecoveryJob(
        IRecoveryService recoveryService,
        ILogger<DailyRecoveryJob> logger)
        : base(logger)
    {
        _recoveryService = recoveryService;
        _logger = logger;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var targetDate = now.Date.AddDays(-1);

        _logger.LogInformation(
            "Daily recovery application targetDate={TargetDate:yyyy-MM-dd}",
            targetDate);

        await _recoveryService.ApplyRecoveryAsync(
            targetDate,
            targetDate,
            null,
            cancellationToken);
    }
}
