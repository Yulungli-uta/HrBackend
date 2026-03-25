using Microsoft.Extensions.Logging;
using Quartz;
using System.Diagnostics;

namespace WsUtaSystem.Infrastructure.Jobs;

public abstract class BaseJob : IJob
{
    protected ILogger Logger { get; }

    protected BaseJob(ILogger logger)
    {
        Logger = logger;
    }

    protected TimeZoneInfo GetTimeZone(IJobExecutionContext context)
    {
        var tzId = context.MergedJobDataMap.GetString("TimeZone") ?? "America/Guayaquil";
        return TimeZoneInfo.FindSystemTimeZoneById(tzId);
    }

    protected DateTime GetCurrentDateTime(IJobExecutionContext context)
    {
        var tz = GetTimeZone(context);
        return TimeZoneInfo.ConvertTime(DateTime.Now, tz);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var runId = Guid.NewGuid().ToString("N");
        var sw = Stopwatch.StartNew();

        Logger.LogInformation(
            "JOB_START runId={RunId} jobKey={JobKey} triggerKey={TriggerKey} fireInstanceId={FireInstanceId} scheduledUtc={ScheduledUtc} firedUtc={FiredUtc}",
            runId,
            context.JobDetail.Key.ToString(),
            context.Trigger.Key.ToString(),
            context.FireInstanceId,
            context.ScheduledFireTimeUtc,
            context.FireTimeUtc
        );

        try
        {
            await ExecuteJobAsync(context, context.CancellationToken);

            sw.Stop();
            Logger.LogInformation(
                "JOB_OK runId={RunId} jobKey={JobKey} durationMs={DurationMs}",
                runId,
                context.JobDetail.Key.ToString(),
                sw.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            Logger.LogError(
                ex,
                "JOB_FAIL runId={RunId} jobKey={JobKey} durationMs={DurationMs}",
                runId,
                context.JobDetail.Key.ToString(),
                sw.ElapsedMilliseconds
            );
            throw;
        }
    }

    protected abstract Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken);
}
