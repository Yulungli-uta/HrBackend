using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs
{
    [DisallowConcurrentExecution]
    public class DailyOvertimeRecoveryCalculation : BaseJob
    {
        private readonly IAttendanceCalculationService _attendanceService;
        private readonly ILogger<DailyNightMinutesCalculationJob> _logger;

        public DailyOvertimeRecoveryCalculation(
            IAttendanceCalculationService attendanceService,
            ILogger<DailyNightMinutesCalculationJob> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var now = GetCurrentDateTime(context);
                var yesterday = now.Date.AddDays(-1);

                _logger.LogInformation("Starting ProcessApplyOvertimeRecovery calculation for date: {Date}", yesterday);

                // Calcular minutos nocturnos del día anterior
                await _attendanceService.ProcessApplyOvertimeRecovery(yesterday, yesterday, null, context.CancellationToken);

                _logger.LogInformation("Night ProcessApplyOvertimeRecovery calculation completed successfully for date: {Date}", yesterday);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing ProcessApplyOvertimeRecovery job");
                throw;
            }
        }
    }
}
