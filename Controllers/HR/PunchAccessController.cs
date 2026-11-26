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
        [FromQuery] string? search, [FromQuery] bool onlyEffective = false, [FromQuery] int? employeeId = null, [FromQuery] Guid? batchId = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _svc.GetPoliciesAsync(search, onlyEffective, employeeId, batchId, page, pageSize, ct));

    [HttpPost("policies")]
    [RequirePermission("PUNCH_POLICY.MANAGE", Strict = true)]
    public async Task<IActionResult> CreatePolicy([FromBody] PunchPolicyCreateDto dto, CancellationToken ct)
    {
        var created = await _svc.CreatePolicyAsync(dto, _currentUser.EmployeeId, ct);
        return created is null ? NotFound() : Ok(created);
    }

    [HttpPost("policies/global")]
    [RequirePermission("PUNCH_POLICY.MANAGE", Strict = true)]
    public async Task<IActionResult> CreateGlobalPolicy([FromBody] PunchPolicyGlobalDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateGlobalPolicyAsync(dto, _currentUser.EmployeeId, ct));

    [HttpGet("policies/department-preview")]
    [RequirePermission("PUNCH_POLICY.MANAGE", Strict = true)]
    public async Task<IActionResult> PreviewDepartment(
        [FromQuery] int departmentId, [FromQuery] bool includeChildren = false, [FromQuery] DateTime? validFrom = null, [FromQuery] DateTime? validTo = null,
        CancellationToken ct = default) =>
        Ok(await _svc.PreviewDepartmentPoliciesAsync(departmentId, includeChildren, validFrom, validTo, ct));

    [HttpPost("policies/department")]
    [RequirePermission("PUNCH_POLICY.MANAGE", Strict = true)]
    public async Task<IActionResult> CreateDepartmentPolicies([FromBody] PunchPolicyDepartmentDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateDepartmentPoliciesAsync(dto, _currentUser.EmployeeId, ct));

    [HttpPost("policies/{policyId:int}/revoke")]
    [RequirePermission("PUNCH_POLICY.MANAGE", Strict = true)]
    public async Task<IActionResult> RevokePolicy([FromRoute] int policyId, [FromBody] PunchPolicyRevokeDto dto, CancellationToken ct)
    {
        var revoked = await _svc.RevokePolicyAsync(policyId, dto, _currentUser.EmployeeId, ct);
        return revoked is null ? NotFound() : Ok(revoked);
    }

    [HttpPost("policies/batch/{batchId:guid}/revoke")]
    [RequirePermission("PUNCH_POLICY.MANAGE", Strict = true)]
    public async Task<IActionResult> RevokeBatch([FromRoute] Guid batchId, [FromBody] PunchPolicyRevokeDto dto, CancellationToken ct)
    {
        var count = await _svc.RevokeBatchAsync(batchId, dto, _currentUser.EmployeeId, ct);
        return count == 0 ? NotFound() : Ok(new { revoked = count });
    }

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
