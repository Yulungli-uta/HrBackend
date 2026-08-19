using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job diario que avanza el estado de los planes de vacaciones masivas por fecha:
/// Planificado -> En Ejecución (con descuento automático de saldo) cuando llega la fecha
/// de inicio, y En Ejecución -> Finalizado cuando pasa la fecha de fin. No hay ejecución
/// manual por botón — la transición siempre la dispara este job.
/// Se ejecuta a la 1:00 AM, antes del cálculo diario de asistencia (7:00 AM), para que el
/// cruce de asistencia del día ya vea los planes recién puestos En Ejecución.
/// </summary>
[DisallowConcurrentExecution]
public sealed class DailyMassVacationPlanTransitionJob : BaseJob
{
    private readonly IMassVacationPlanService _massVacationPlanService;
    private readonly ILogger<DailyMassVacationPlanTransitionJob> _logger;

    public DailyMassVacationPlanTransitionJob(
        IMassVacationPlanService massVacationPlanService,
        ILogger<DailyMassVacationPlanTransitionJob> logger,
        IJobExecutionLogService jobExecutionLogService)
        : base(logger, jobExecutionLogService)
    {
        _massVacationPlanService = massVacationPlanService ?? throw new ArgumentNullException(nameof(massVacationPlanService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteJobAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var now = GetCurrentDateTime(context);
        _logger.LogInformation(
            "Inicio transición diaria de planes de vacaciones masivas. FechaLocal={Date:yyyy-MM-dd HH:mm}", now);

        var result = await _massVacationPlanService.ProcessDueTransitionsAsync(null, cancellationToken);

        _logger.LogInformation(
            "Transición diaria de planes de vacaciones masivas finalizada: {StartedCount} planes pasaron a En Ejecución, {FinishedCount} pasaron a Finalizado.",
            result.StartedPlans.Count,
            result.FinishedPlanIds.Count);
    }
}
