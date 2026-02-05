using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DailyOvertimeRecoveryCalculation : BaseJob
{
    private readonly IAttendanceCalculationService _attendanceService;
    private readonly ILogger<DailyOvertimeRecoveryCalculation> _logger;
    public DailyOvertimeRecoveryCalculation(
        IAttendanceCalculationService attendanceService,
        ILogger<DailyOvertimeRecoveryCalculation> logger)
        : base(logger)
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
