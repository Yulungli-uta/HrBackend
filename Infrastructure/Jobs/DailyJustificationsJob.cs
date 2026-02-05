using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DailyJustificationsJob : BaseJob
{
    private readonly IJustificationsService _justificationsService;
    private readonly ILogger<DailyJustificationsJob> _logger;

    public DailyJustificationsJob(
        IJustificationsService justificationsService,
        ILogger<DailyJustificationsJob> logger)
        : base(logger)
    {
        _justificationsService = justificationsService;
        _logger = logger;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var targetDate = now.Date.AddDays(-1);

        Logger.LogInformation(
            "Daily justifications targetDate={TargetDate:yyyy-MM-dd}",
            targetDate);

        await _justificationsService.ApplyJustificationsAsync(
            targetDate,
            targetDate,
            null,
            cancellationToken);
    }
}
