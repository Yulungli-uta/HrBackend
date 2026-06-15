using WsUtaSystem.Application.DTOs.Provisioning;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Cliente HTTP para el servicio de aprovisionamiento de empleados en RepositoryUta.
/// Convierte empleados HR en cuentas AD Local con posterior sincronización a Entra ID y O365.
/// </summary>
public interface IEmployeeProvisioningClient
{
    /// <summary>
    /// Solicita el aprovisionamiento de un empleado en RepositoryUta.
    /// Retorna null si el servicio no está disponible o el empleado ya está aprovisionado.
    /// </summary>
    /// <param name="req">Datos del empleado a aprovisionar.</param>
    /// <param name="bearerToken">Token JWT del usuario que ejecuta la acción (se reenvía al servicio).</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<HrProvisioningResult?> ProvisionAsync(
        HrProvisionEmployeeRequest req,
        string bearerToken,
        CancellationToken ct = default);

    /// <summary>
    /// Deshabilita la cuenta institucional de un empleado en RepositoryUta
    /// (AD Local + auth.tbl_Users.IsActive = false).
    /// </summary>
    Task<HrDisableEmployeeResult?> DisableAsync(
        int hrEmployeeId,
        string bearerToken,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene un JWT haciendo login con las credenciales de la cuenta de servicio
    /// configuradas en <c>AuthService:ServiceAccount</c>. Útil para jobs en background
    /// que no tienen contexto HTTP. Retorna null si las credenciales no están configuradas
    /// o el login falla.
    /// </summary>
    Task<string?> GetServiceTokenAsync(CancellationToken ct = default);
}
