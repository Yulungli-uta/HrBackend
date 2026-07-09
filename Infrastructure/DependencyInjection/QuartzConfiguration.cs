using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using WsUtaSystem.Infrastructure.Jobs;

namespace WsUtaSystem.Infrastructure.DependencyInjection;

/// <summary>
/// Configuración de Quartz.NET para jobs automáticos.
/// Los jobs solo se registran si Quartz:EnableJobs = true en la configuración,
/// lo que previene que instancias locales de desarrollo disparen jobs de producción.
/// </summary>
public static class QuartzConfiguration
{
    public static IServiceCollection AddQuartzJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var enableJobs = configuration.GetValue<bool>("Quartz:EnableJobs");

        if (!enableJobs)
            return services;

        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            const string timeZone = "America/Guayaquil";

            // ========================================
            // JOBS DIARIOS DE ASISTENCIA
            // ========================================

            // 1. Cálculo de asistencia diario - 7:00 AM
            // Nota (2026-07-03): se había migrado a SQL Server Agent (ver
            // Database/SqlAgent_DailyAttendanceJob.sql), pero el servicio "SQL Server
            // Agent" está detenido en el servidor y no hay forma inmediata de
            // reactivarlo, así que se revierte a correr desde el backend vía Quartz.
            // Si en algún momento se reactiva SQL Server Agent, volver a comentar este
            // bloque para evitar doble ejecución.
            var dailyAttendanceKey = new JobKey("DailyAttendanceCalculationJob");
            q.AddJob<DailyAttendanceCalculationJob>(opts => opts.WithIdentity(dailyAttendanceKey));
            q.AddTrigger(opts => opts
                .ForJob(dailyAttendanceKey)
                .WithIdentity("DailyAttendanceCalculationTrigger")
                .WithCronSchedule("0 0 7 * * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Ejecuta el cálculo de asistencia diariamente a las 7:00 AM (permite capturar picadas de turnos nocturnos hasta las 06:00)")
                .UsingJobData("TimeZone", timeZone));

            // 2. Contratos vencidos - diariamente a las 2:00 AM
            var contractExpirationKey = new JobKey("DailyContractExpirationJob");
            q.AddJob<DailyContractExpirationJob>(opts => opts.WithIdentity(contractExpirationKey));
            q.AddTrigger(opts => opts
                .ForJob(contractExpirationKey)
                .WithIdentity("DailyContractExpirationTrigger")
                .WithCronSchedule("0 0 2 * * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Detecta contratos VIGENTES vencidos: marca VENCIDO y deshabilita cuentas AD")
                .UsingJobData("TimeZone", timeZone));

            // 3. Sincronización de matrículas de estudiantes — manual/bajo demanda (no hay cron fijo)
            // El PeriodCode y PreviousPeriod se inyectan desde el endpoint manual o se configuran aquí.
            var studentEnrollmentKey = new JobKey("DailyStudentEnrollmentSyncJob");
            q.AddJob<DailyStudentEnrollmentSyncJob>(opts => opts
                .WithIdentity(studentEnrollmentKey)
                .StoreDurably()   // permite dispararlo manualmente sin trigger fijo
                .UsingJobData("PeriodCode", "")
                .UsingJobData("PreviousPeriod", ""));

            // 4. Acreditacion de vacaciones - Día 1 del mes a las 00:30
            var accrueVacation = new JobKey("MonthlyAccrueVacationBalanceJob");
            q.AddJob<DailyAccrueVacationBalance>(opts => opts.WithIdentity(accrueVacation));
            q.AddTrigger(opts => opts
                .ForJob(accrueVacation)
                .WithIdentity("MonthlyAccrueVacationBalanceTrigger")
                .WithCronSchedule("0 30 0 1 * ?", x => x
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
                .WithDescription("Acredita vacaciones mensual el día 1 (acredita el mes anterior)")
                .UsingJobData("TimeZone", timeZone));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}
