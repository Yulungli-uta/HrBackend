using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// OBSOLETO (verificado 2026-07-22): NO está registrado en QuartzConfiguration.cs — nunca se
/// ejecuta. Llama a <see cref="IAttendanceCalculationService.ProcessApplyOvertimeRecovery"/>
/// (también obsoleto). El pipeline diario real (sp_ProcessAttendancePlanningDay, etapa 5) ya
/// cubre horas extra/recuperación vía HR.tbl_TimePlanning. No registrar este job.
/// </summary>
[Obsolete("No registrado en Quartz, nunca se ejecuta — el pipeline diario real ya cubre esto vía sp_ProcessAttendancePlanningDay.")]
[DisallowConcurrentExecution]
public sealed class DailyOvertimeRecoveryCalculation : BaseJob
{
    private readonly IAttendanceCalculationService _attendanceService;
    private readonly ILogger<DailyOvertimeRecoveryCalculation> _logger;
    public DailyOvertimeRecoveryCalculation(
        IAttendanceCalculationService attendanceService,
        ILogger<DailyOvertimeRecoveryCalculation> logger,
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
            "Daily overtime recovery targetDate={TargetDate:yyyy-MM-dd}",
            targetDate);

        await _attendanceService.ProcessApplyOvertimeRecovery(
            targetDate,
            targetDate,
            null,
            cancellationToken);
    }
}
