using Quartz;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Clase base para todos los Jobs de Quartz.NET
/// Proporciona funcionalidad común para obtener la zona horaria y fecha actual
/// </summary>
public abstract class BaseJob : IJob
{
    protected TimeZoneInfo GetTimeZone(IJobExecutionContext context)
    {
        var tzId = context.MergedJobDataMap.GetString("TimeZone") ?? "America/Guayaquil";
        return TimeZoneInfo.FindSystemTimeZoneById(tzId);
    }

    protected DateTime GetCurrentDateTime(IJobExecutionContext context)
    {
        var tz = GetTimeZone(context);
        return TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
    }

    public abstract Task Execute(IJobExecutionContext context);
}

