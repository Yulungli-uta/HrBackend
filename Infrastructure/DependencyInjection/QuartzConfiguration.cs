using Microsoft.Extensions.DependencyInjection;
using Quartz;
using WsUtaSystem.Infrastructure.Jobs;

namespace WsUtaSystem.Infrastructure.DependencyInjection;

/// <summary>
/// Configuración de Quartz.NET para jobs automáticos
/// </summary>
public static class QuartzConfiguration
{
    public static IServiceCollection AddQuartzJobs(this IServiceCollection services)
    {
        // Configurar Quartz.NET
        services.AddQuartz(q =>
        {
            // Usar Microsoft DI para inyección de dependencias en Jobs
            q.UseMicrosoftDependencyInjectionJobFactory();

            // Zona horaria por defecto
            const string timeZone = "America/Guayaquil";

            // ========================================
            // JOBS DIARIOS DE ASISTENCIA
            // ========================================

            // 1. Cálculo de asistencia diario - 2:00 AM
            var dailyAttendanceKey = new JobKey("DailyAttendanceCalculationJob");
            q.AddJob<DailyAttendanceCalculationJob>(opts => opts.WithIdentity(dailyAttendanceKey));
            q.AddTrigger(opts => opts
                .ForJob(dailyAttendanceKey)
                .WithIdentity("DailyAttendanceCalculationTrigger")
                .WithCronSchedule("0 0 2 * * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Ejecuta el cálculo de asistencia diariamente a las 2:00 AM")
                .UsingJobData("TimeZone", timeZone));

            // 2. Cálculo de minutos nocturnos - 3:00 AM
            var nightMinutesKey = new JobKey("DailyNightMinutesCalculationJob");
            q.AddJob<DailyNightMinutesCalculationJob>(opts => opts.WithIdentity(nightMinutesKey));
            q.AddTrigger(opts => opts
                .ForJob(nightMinutesKey)
                .WithIdentity("DailyNightMinutesCalculationTrigger")
                .WithCronSchedule("0 0 3 * * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Ejecuta el cálculo de minutos nocturnos diariamente a las 3:00 AM")
                .UsingJobData("TimeZone", timeZone));

            // 3. Aplicar justificaciones - 4:00 AM
            var justificationsKey = new JobKey("DailyJustificationsJob");
            q.AddJob<DailyJustificationsJob>(opts => opts.WithIdentity(justificationsKey));
            q.AddTrigger(opts => opts
                .ForJob(justificationsKey)
                .WithIdentity("DailyJustificationsTrigger")
                .WithCronSchedule("0 0 4 * * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Aplica justificaciones aprobadas diariamente a las 4:00 AM")
                .UsingJobData("TimeZone", timeZone));

            // 4. Aplicar recuperaciones - 5:00 AM
            var recoveryKey = new JobKey("DailyRecoveryJob");
            q.AddJob<DailyRecoveryJob>(opts => opts.WithIdentity(recoveryKey));
            q.AddTrigger(opts => opts
                .ForJob(recoveryKey)
                .WithIdentity("DailyRecoveryTrigger")
                .WithCronSchedule("0 0 5 * * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Aplica recuperaciones de tiempo diariamente a las 5:00 AM")
                .UsingJobData("TimeZone", timeZone));

            // ========================================
            // JOBS MENSUALES DE NÓMINA
            // ========================================

            // 5. Cálculo de horas extra - Día 1 del mes a las 2:00 AM
            var overtimeKey = new JobKey("MonthlyOvertimePriceJob");
            q.AddJob<MonthlyOvertimePriceJob>(opts => opts.WithIdentity(overtimeKey));
            q.AddTrigger(opts => opts
                .ForJob(overtimeKey)
                .WithIdentity("MonthlyOvertimePriceTrigger")
                .WithCronSchedule("0 0 2 1 * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Calcula precio de horas extra el día 1 de cada mes a las 2:00 AM")
                .UsingJobData("TimeZone", timeZone));

            // 6. Cálculo de descuentos - Día 1 del mes a las 3:00 AM
            var discountsKey = new JobKey("MonthlyPayrollDiscountsJob");
            q.AddJob<MonthlyPayrollDiscountsJob>(opts => opts.WithIdentity(discountsKey));
            q.AddTrigger(opts => opts
                .ForJob(discountsKey)
                .WithIdentity("MonthlyPayrollDiscountsTrigger")
                .WithCronSchedule("0 0 3 1 * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Calcula descuentos de nómina el día 1 de cada mes a las 3:00 AM")
                .UsingJobData("TimeZone", timeZone));

            // 7. Cálculo de subsidios - Día 1 del mes a las 4:00 AM
            var subsidiesKey = new JobKey("MonthlyPayrollSubsidiesJob");
            q.AddJob<MonthlyPayrollSubsidiesJob>(opts => opts.WithIdentity(subsidiesKey));
            q.AddTrigger(opts => opts
                .ForJob(subsidiesKey)
                .WithIdentity("MonthlyPayrollSubsidiesTrigger")
                .WithCronSchedule("0 0 4 1 * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Calcula subsidios de nómina el día 1 de cada mes a las 4:00 AM")
                .UsingJobData("TimeZone", timeZone));
        });

        // Agregar el servicio de Quartz hosted
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}

