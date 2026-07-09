using Microsoft.Extensions.Logging;
using Quartz;
using System.Diagnostics;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

public abstract class BaseJob : IJob
{
    protected ILogger Logger { get; }
    protected IJobExecutionLogService JobExecutionLogService { get; }

    protected BaseJob(ILogger logger, IJobExecutionLogService jobExecutionLogService)
    {
        Logger = logger;
        JobExecutionLogService = jobExecutionLogService;
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
        var jobName = context.JobDetail.Key.ToString();

        Logger.LogInformation(
            "JOB_START runId={RunId} jobKey={JobKey} triggerKey={TriggerKey} fireInstanceId={FireInstanceId} scheduledUtc={ScheduledUtc} firedUtc={FiredUtc}",
            runId,
            jobName,
            context.Trigger.Key.ToString(),
            context.FireInstanceId,
            context.ScheduledFireTimeUtc,
            context.FireTimeUtc
        );

        // El logging de auditoría nunca debe tumbar la ejecución real del job.
        long? logId = null;
        try
        {
            logId = await JobExecutionLogService.StartAsync(jobName, "Quartz", context.CancellationToken);
        }
        catch (Exception logEx)
        {
            Logger.LogWarning(logEx, "JOB_EXECUTION_LOG_START_FAIL runId={RunId} jobKey={JobKey}", runId, jobName);
        }

        try
        {
            await ExecuteJobAsync(context, context.CancellationToken);

            sw.Stop();
            Logger.LogInformation(
                "JOB_OK runId={RunId} jobKey={JobKey} durationMs={DurationMs}",
                runId,
                jobName,
                sw.ElapsedMilliseconds
            );

            if (logId.HasValue)
            {
                try
                {
                    await JobExecutionLogService.FinishAsync(logId.Value, "Success", null, context.CancellationToken);
                }
                catch (Exception logEx)
                {
                    Logger.LogWarning(logEx, "JOB_EXECUTION_LOG_FINISH_FAIL runId={RunId} jobKey={JobKey}", runId, jobName);
                }
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            Logger.LogError(
                ex,
                "JOB_FAIL runId={RunId} jobKey={JobKey} durationMs={DurationMs}",
                runId,
                jobName,
                sw.ElapsedMilliseconds
            );

            if (logId.HasValue)
            {
                try
                {
                    await JobExecutionLogService.FinishAsync(logId.Value, "Failed", ex.Message, context.CancellationToken);
                }
                catch (Exception logEx)
                {
                    Logger.LogWarning(logEx, "JOB_EXECUTION_LOG_FINISH_FAIL runId={RunId} jobKey={JobKey}", runId, jobName);
                }
            }

            throw;
        }
    }

    protected abstract Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken);
}
