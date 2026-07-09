namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Registra el inicio y fin de ejecuciones de jobs (Quartz y, a futuro, SQL Server Agent)
/// en HR.tbl_JobExecutionLog para trazabilidad y auditoría operativa.
/// </summary>
public interface IJobExecutionLogService
{
    /// <summary>Registra el inicio de una ejecución de job y devuelve el ID del log generado.</summary>
    Task<long> StartAsync(string jobName, string source, CancellationToken ct = default);

    /// <summary>Cierra el registro de ejecución de un job con su resultado final.</summary>
    Task FinishAsync(long logId, string status, string? errorMessage = null, CancellationToken ct = default);
}
