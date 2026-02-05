using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DailyAccrueVacationBalance : BaseJob
{
    private readonly ITimeBalancesService _timeService;

    private readonly ILogger<DailyAccrueVacationBalance> _logger;

    private readonly ICurrentUserService _currentUserService;

    public DailyAccrueVacationBalance(
        ITimeBalancesService timeService,
        ILogger<DailyAccrueVacationBalance> logger,
        ICurrentUserService currentUserService)
        : base(logger)
    {
        _timeService = timeService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        //var targetDate = now.Date.AddDays(-1); // Ejecutar para el día anterior
        
        // Último día del mes anterior
        var targetDate = new DateTime(now.Year, now.Month, 1).AddDays(-1);

        _logger.LogInformation(
            "Daily accrue vacation balance targetDate={TargetDate:yyyy-MM-dd}",
            targetDate);
  
        await _timeService.CalculateAccrueVacationBalanceAllEmployees(            
            asOfDate: targetDate,
            //mode: "TOTAL",    // calcula para modo TOTAL es decir desde que se contrato la persona agregar 
            //mode: "DAILY",      // calcula solo la proporción diaria para agregar
            mode: "MONTHLY",   // calcula solo la proporción mensual para agregar
            performedByEmpId: _currentUserService.EmployeeId,
            ct: cancellationToken
        );
    }
}
