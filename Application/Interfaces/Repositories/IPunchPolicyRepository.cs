using WsUtaSystem.Application.DTOs.PunchPolicy;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Acceso a datos del control de picadas por red (redes del campus, permisos externos, registro).</summary>
public interface IPunchPolicyRepository
{
    // Redes del campus (catálogo pequeño: se lista completo)
    Task<List<AllowedPunchNetwork>> GetNetworksAsync(CancellationToken ct);
    Task<AllowedPunchNetwork?> GetNetworkAsync(int networkId, CancellationToken ct);
    Task<bool> NetworkExistsAsync(string ipOrCidr, int? excludeNetworkId, CancellationToken ct);
    Task<AllowedPunchNetwork> AddNetworkAsync(AllowedPunchNetwork network, CancellationToken ct);
    Task UpdateNetworkAsync(AllowedPunchNetwork network, CancellationToken ct);
    Task<bool> DeleteNetworkAsync(int networkId, CancellationToken ct);

    // Permisos de picada externa por empleado
    Task<EmployeePunchPolicy?> GetPolicyAsync(int employeeId, CancellationToken ct);
    Task<(List<PunchPolicyItemDto> Items, long Total)> GetPoliciesPagedAsync(
        string? search, bool onlyEffective, DateTime today, int page, int pageSize, CancellationToken ct);
    Task<PunchPolicyItemDto?> GetPolicyItemAsync(int employeeId, DateTime today, CancellationToken ct);
    Task UpsertPolicyAsync(EmployeePunchPolicy policy, CancellationToken ct);
    Task<bool> DeletePolicyAsync(int employeeId, CancellationToken ct);
    Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken ct);

    // Registro de picadas fuera del campus
    Task AddLogAsync(PunchAccessLog log, CancellationToken ct);
    Task<(List<PunchAccessLogItemDto> Items, long Total)> GetLogPagedAsync(
        DateTime? from, DateTime? to, int? employeeId, string? result, int page, int pageSize, CancellationToken ct);

    // Modo de operación (HR.tbl_Parameters: PUNCH_NETWORK_MODE)
    Task<string?> GetModeAsync(CancellationToken ct);
    /// <summary>Valor de un parámetro activo de HR.tbl_Parameters (null si no existe).</summary>
    Task<string?> GetParameterAsync(string name, CancellationToken ct);
    Task SetModeAsync(string mode, CancellationToken ct);
}
