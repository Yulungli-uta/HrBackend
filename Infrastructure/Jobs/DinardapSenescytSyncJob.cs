using Microsoft.Extensions.Logging;
using Quartz;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Jobs;

/// <summary>
/// Job programado que sincroniza Formación Académica (HR.tbl_EducationLevels) contra DINARDAP
/// (Títulos SENESCYT, vía WsUtaDinardap.Api) para todos los empleados activos. Dedup por
/// SenescytRegistrationNumber - solo crea los títulos que no existían todavía. Un fallo
/// puntual de DINARDAP para una persona se omite y continúa con las demás (ver
/// EducationLevelSyncService.SyncActiveEmployeesAsync). Cron configurable, deshabilitado por
/// defecto hasta que el usuario confirme la periodicidad deseada.
/// </summary>
[DisallowConcurrentExecution]
public sealed class DinardapSenescytSyncJob : BaseJob
{
    private readonly IEducationLevelSyncService _syncService;
    private readonly ILogger<DinardapSenescytSyncJob> _logger;

    public DinardapSenescytSyncJob(
        IEducationLevelSyncService syncService,
        ILogger<DinardapSenescytSyncJob> logger,
        IJobExecutionLogService jobExecutionLogService)
        : base(logger, jobExecutionLogService)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteJobAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[DINARDAP-SENESCYT-JOB] Inicio sincronización masiva de empleados activos.");

        var result = await _syncService.SyncActiveEmployeesAsync(cancellationToken);

        _logger.LogInformation(
            "[DINARDAP-SENESCYT-JOB] Completado: {Procesadas} procesada(s), {ConError} con error, {Creados} título(s) nuevo(s) en total.",
            result.PersonasProcesadas, result.PersonasConError, result.TitulosCreadosTotal);
    }
}
