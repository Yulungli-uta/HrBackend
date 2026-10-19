using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.PunchPolicy;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Control de picadas por red: redes del campus y modo de operación (PUNCH_NETWORK.MANAGE: DITIC y administradores),
/// permisos de picada externa por empleado (PUNCH_POLICY.MANAGE: administradores y RRHH) y registro (PUNCH_POLICY.READ).
/// Los permisos se exigen en modo estricto (no dependen de Authorization:ShadowMode): son endpoints
/// nuevos y deciden quién puede picar fuera del campus.
/// </summary>
[ApiController]
[Route("attendance/punch-access")]
public sealed class PunchAccessController : ControllerBase
{
    private readonly IPunchAccessPolicyService _svc;
    private readonly ICurrentUserService _currentUser;

    public PunchAccessController(IPunchAccessPolicyService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    /// <summary>Lo que el servidor ve del usuario autenticado: su IP, si está en el campus y si puede picar desde fuera.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyAccess(CancellationToken ct)
    {
        // Un usuario sin empleado asociado (p. ej. un administrador) también ve su IP; simplemente no tiene permiso de picada.
        return Ok(await _svc.GetMyAccessAsync(_currentUser.EmployeeId ?? 0, _currentUser.GetRemoteIp(), ct));
    }

    // ── Redes del campus ────────────────────────────────────────────────────

    [HttpGet("networks")]
    [RequirePermission("PUNCH_POLICY.READ", Strict = true)]
    public async Task<IActionResult> GetNetworks(CancellationToken ct) => Ok(await _svc.GetNetworksAsync(ct));

    [HttpPost("networks")]
    [RequirePermission("PUNCH_NETWORK.MANAGE", Strict = true)]
    public async Task<IActionResult> CreateNetwork([FromBody] PunchNetworkSaveDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateNetworkAsync(dto, _currentUser.EmployeeId, ct));

    [HttpPut("networks/{id:int}")]
    [RequirePermission("PUNCH_NETWORK.MANAGE", Strict = true)]
    public async Task<IActionResult> UpdateNetwork([FromRoute] int id, [FromBody] PunchNetworkSaveDto dto, CancellationToken ct)
    {
        var updated = await _svc.UpdateNetworkAsync(id, dto, _currentUser.EmployeeId, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("networks/{id:int}")]
    [RequirePermission("PUNCH_NETWORK.MANAGE", Strict = true)]
    public async Task<IActionResult> DeleteNetwork([FromRoute] int id, CancellationToken ct) =>
        await _svc.DeleteNetworkAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>Prueba si una IP pertenece a alguna red activa del campus.</summary>
    [HttpGet("check")]
    [RequirePermission("PUNCH_POLICY.READ", Strict = true)]
    public async Task<IActionResult> CheckIp([FromQuery] string ip, CancellationToken ct) => Ok(await _svc.CheckIpAsync(ip, ct));

    // ── Permiso de picada externa por empleado ──────────────────────────────

    [HttpGet("policies")]
    [RequirePermission("PUNCH_POLICY.READ", Strict = true)]
    public async Task<IActionResult> GetPolicies(
        [FromQuery] string? search, [FromQuery] bool onlyEffective = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _svc.GetPoliciesAsync(search, onlyEffective, page, pageSize, ct));

    [HttpPut("policies/{employeeId:int}")]
    [RequirePermission("PUNCH_POLICY.MANAGE", Strict = true)]
    public async Task<IActionResult> SavePolicy([FromRoute] int employeeId, [FromBody] PunchPolicySaveDto dto, CancellationToken ct)
    {
        var saved = await _svc.SavePolicyAsync(employeeId, dto, _currentUser.EmployeeId, ct);
        return saved is null ? NotFound() : Ok(saved);
    }

    [HttpDelete("policies/{employeeId:int}")]
    [RequirePermission("PUNCH_POLICY.MANAGE", Strict = true)]
    public async Task<IActionResult> DeletePolicy([FromRoute] int employeeId, CancellationToken ct) =>
        await _svc.DeletePolicyAsync(employeeId, ct) ? NoContent() : NotFound();

    // ── Registro de picadas fuera del campus ────────────────────────────────

    [HttpGet("log")]
    [RequirePermission("PUNCH_POLICY.READ", Strict = true)]
    public async Task<IActionResult> GetLog(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? employeeId, [FromQuery] string? result,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _svc.GetLogAsync(from, to, employeeId, result, page, pageSize, ct));

    // ── Modo de operación ───────────────────────────────────────────────────

    [HttpGet("settings")]
    [RequirePermission("PUNCH_POLICY.READ", Strict = true)]
    public async Task<IActionResult> GetSettings(CancellationToken ct) => Ok(await _svc.GetSettingsAsync(ct));

    [HttpPut("settings")]
    [RequirePermission("PUNCH_NETWORK.MANAGE", Strict = true)]
    public async Task<IActionResult> SetMode([FromBody] PunchSettingsDto dto, CancellationToken ct) =>
        Ok(await _svc.SetModeAsync(dto.Mode, ct));
}
