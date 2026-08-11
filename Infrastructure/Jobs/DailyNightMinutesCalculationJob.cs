using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// OBSOLETO (verificado 2026-07-22): NO está registrado en QuartzConfiguration.cs — nunca se
/// ejecuta. Llama a <see cref="IAttendanceCalculationService.CalculateNightMinutesAsync"/>
/// (también obsoleto). El único job de asistencia registrado es
/// <see cref="DailyAttendanceCalculationJob"/> (pipeline de 6 etapas). No registrar este job
/// sin antes confirmar si el cálculo nocturno ya vive en ese pipeline o si falta.
/// </summary>
[Obsolete("No registrado en Quartz, nunca se ejecuta. Ver DailyAttendanceCalculationJob para el pipeline real vigente.")]
[DisallowConcurrentExecution]
public sealed class DailyNightMinutesCalculationJob : BaseJob
{
    private readonly IAttendanceCalculationService _attendanceService;
    private readonly ILogger<DailyNightMinutesCalculationJob> _logger;
    public DailyNightMinutesCalculationJob(
        IAttendanceCalculationService attendanceService,
        ILogger<DailyNightMinutesCalculationJob> logger,
        IJobExecutionLogService jobExecutionLogService)
        : base(logger, jobExecutionLogService)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        var targetDate = now.Date.AddDays(-1);

        _logger.LogInformation(
            "Daily night minutes calculation targetDate={TargetDate:yyyy-MM-dd}",
            targetDate);

        await _attendanceService.CalculateNightMinutesAsync(
            targetDate,
            targetDate,
            null,
            cancellationToken);
    }
}
