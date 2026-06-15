namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Detecta contratos VIGENTES cuya fecha de fin ya pasó (sin addenda activa)
/// y ejecuta las acciones de cierre: marcar VENCIDO y deshabilitar cuenta AD
/// cuando el ContractType.RequiresAdUserDisable = true.
/// Diseñado para ser invocado desde un job de Quartz en background.
/// </summary>
public interface IContractExpirationService
{
    /// <summary>
    /// Procesa contratos vencidos. Retorna el número de contratos procesados.
    /// </summary>
    /// <param name="serviceToken">JWT obtenido con la cuenta de servicio para llamar RepositoryUta.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<int> ProcessExpiredContractsAsync(string serviceToken, CancellationToken ct = default);
}
