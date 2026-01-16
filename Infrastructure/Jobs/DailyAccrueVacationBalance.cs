using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs
{
    [DisallowConcurrentExecution]
    public class DailyAccrueVacationBalance : BaseJob
    {
        private readonly ITimeBalancesService _timeService;
        private readonly ILogger<DailyAccrueVacationBalance> _logger;

        public DailyAccrueVacationBalance(
            ITimeBalancesService attendanceService,
            ILogger<DailyAccrueVacationBalance> logger)
        {
            _timeService = attendanceService;
            _logger = logger;
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var now = GetCurrentDateTime(context);
                var yesterday = now.Date.AddDays(-1);

                _logger.LogInformation("Starting daily AccrueVacationBalance for date: {Date}", yesterday);

                // Calcular asistencia del día anterior
                //await _attendanceService.CalculateRangeAsync(yesterday, yesterday, null, context.CancellationToken);
                await _timeService.CalculateAccrueVacationBalance(yesterday, yesterday, null, context.CancellationToken);

                _logger.LogInformation("Daily AccrueVacationBalance completed successfully for date: {Date}", yesterday);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing daily AccrueVacationBalance job");
                throw;
            }
        }
    }  

}
