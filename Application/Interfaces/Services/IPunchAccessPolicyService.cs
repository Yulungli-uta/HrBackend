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

    /// <summary>
    /// Registra en la bitácora de picadas un evento del flujo (rechazo del servidor, error o evento informado por la
    /// pantalla). Nunca lanza: un fallo de bitácora no debe impedir la picada. <paramref name="mode"/> es SERVER o CLIENT.
    /// </summary>
    Task LogEventAsync(int employeeId, string? clientIp, string? forwardedFor, string result, string mode, string? detail, CancellationToken ct);

    /// <summary>Lo que el servidor ve del usuario: su IP, si está en el campus y si tiene permiso externo.</summary>
    Task<PunchAccessInfoDto> GetMyAccessAsync(int employeeId, string? clientIp, CancellationToken ct);

    Task<IReadOnlyList<PunchNetworkDto>> GetNetworksAsync(CancellationToken ct);
    Task<PunchNetworkDto> CreateNetworkAsync(PunchNetworkSaveDto dto, int? userEmployeeId, CancellationToken ct);
    Task<PunchNetworkDto?> UpdateNetworkAsync(int networkId, PunchNetworkSaveDto dto, int? userEmployeeId, CancellationToken ct);
    Task<bool> DeleteNetworkAsync(int networkId, CancellationToken ct);
    Task<PunchIpCheckDto> CheckIpAsync(string ip, CancellationToken ct);

    Task<PagedResult<PunchPolicyItemDto>> GetPoliciesAsync(string? search, bool onlyEffective, int? employeeId, Guid? batchId, int page, int pageSize, CancellationToken ct);
    /// <summary>Crea una autorización individual (no se edita: se revoca y se crea otra). Null si el empleado no existe.</summary>
    Task<PunchPolicyItemDto?> CreatePolicyAsync(PunchPolicyCreateDto dto, int? authorizedBy, CancellationToken ct);
    /// <summary>Crea UNA autorización global (toda la institución, sin empleado). Exige fecha final y no se cruza con otra global.</summary>
    Task<PunchPolicyItemDto> CreateGlobalPolicyAsync(PunchPolicyGlobalDto dto, int? authorizedBy, CancellationToken ct);
    /// <summary>Cuántos empleados alcanzaría una autorización por departamento y a cuántos se omitiría (no escribe).</summary>
    Task<PunchPolicyDepartmentPreviewDto> PreviewDepartmentPoliciesAsync(int departmentId, bool includeChildren, DateTime? validFrom, DateTime? validTo, CancellationToken ct);
    /// <summary>Crea una autorización por cada empleado activo del departamento (y sus hijos si se pide), en un mismo lote.</summary>
    Task<PunchPolicyBatchResultDto> CreateDepartmentPoliciesAsync(PunchPolicyDepartmentDto dto, int? authorizedBy, CancellationToken ct);
    /// <summary>Revoca una autorización sin borrarla. Null si no existe.</summary>
    Task<PunchPolicyItemDto?> RevokePolicyAsync(int policyId, PunchPolicyRevokeDto dto, int? revokedBy, CancellationToken ct);
    /// <summary>Revoca todas las autorizaciones aún abiertas de un lote; devuelve cuántas.</summary>
    Task<int> RevokeBatchAsync(Guid batchId, PunchPolicyRevokeDto dto, int? revokedBy, CancellationToken ct);

    Task<PagedResult<PunchAccessLogItemDto>> GetLogAsync(DateTime? from, DateTime? to, int? employeeId, string? result, int page, int pageSize, CancellationToken ct);

    Task<PunchSettingsDto> GetSettingsAsync(CancellationToken ct);

    /// <summary>Minutos mínimos entre dos picadas del mismo empleado (parámetro PUNCH_MIN_INTERVAL_MINUTES; 0 = sin regla).</summary>
    Task<int> GetMinIntervalMinutesAsync(CancellationToken ct);
    Task<PunchSettingsDto> SetModeAsync(string mode, CancellationToken ct);
}
