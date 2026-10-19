using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.PunchPolicy;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Control de picadas por red: decide si una picada puede registrarse desde la IP de origen y
/// administra la lista de redes del campus, los permisos de picada externa y su registro.
/// </summary>
public interface IPunchAccessPolicyService
{
    /// <summary>
    /// Evalúa si el empleado puede picar desde esa IP según el modo configurado (OFF / AUDIT / ENFORCE).
    /// En AUDIT nunca bloquea: solo registra lo que se habría rechazado.
    /// </summary>
    Task<PunchAccessDecision> EvaluateAsync(int employeeId, string? clientIp, string? forwardedFor, CancellationToken ct);

    /// <summary>Lo que el servidor ve del usuario: su IP, si está en el campus y si tiene permiso externo.</summary>
    Task<PunchAccessInfoDto> GetMyAccessAsync(int employeeId, string? clientIp, CancellationToken ct);

    Task<IReadOnlyList<PunchNetworkDto>> GetNetworksAsync(CancellationToken ct);
    Task<PunchNetworkDto> CreateNetworkAsync(PunchNetworkSaveDto dto, int? userEmployeeId, CancellationToken ct);
    Task<PunchNetworkDto?> UpdateNetworkAsync(int networkId, PunchNetworkSaveDto dto, int? userEmployeeId, CancellationToken ct);
    Task<bool> DeleteNetworkAsync(int networkId, CancellationToken ct);
    Task<PunchIpCheckDto> CheckIpAsync(string ip, CancellationToken ct);

    Task<PagedResult<PunchPolicyItemDto>> GetPoliciesAsync(string? search, bool onlyEffective, int page, int pageSize, CancellationToken ct);
    Task<PunchPolicyItemDto?> SavePolicyAsync(int employeeId, PunchPolicySaveDto dto, int? authorizedBy, CancellationToken ct);
    Task<bool> DeletePolicyAsync(int employeeId, CancellationToken ct);

    Task<PagedResult<PunchAccessLogItemDto>> GetLogAsync(DateTime? from, DateTime? to, int? employeeId, string? result, int page, int pageSize, CancellationToken ct);

    Task<PunchSettingsDto> GetSettingsAsync(CancellationToken ct);

    /// <summary>Minutos mínimos entre dos picadas del mismo empleado (parámetro PUNCH_MIN_INTERVAL_MINUTES; 0 = sin regla).</summary>
    Task<int> GetMinIntervalMinutesAsync(CancellationToken ct);
    Task<PunchSettingsDto> SetModeAsync(string mode, CancellationToken ct);
}
