using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-shift-changes")]
public class GuardShiftChangesController : ControllerBase
{
    private readonly IGuardShiftChangeService _svc;
    public GuardShiftChangesController(IGuardShiftChangeService svc) => _svc = svc;

    [HttpGet("pending")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetPending(CancellationToken ct) =>
        Ok(await _svc.GetPendingAsync(ct));

    [HttpGet("pending/paged")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetPendingPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetPendingPagedAsync(page, pageSize, ct));
    }

    [HttpGet("all/paged")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetAllPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetAllPagedAsync(page, pageSize, status, ct));
    }

    [HttpGet("by-planning/{planningId:int}")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetByPlanning(int planningId, CancellationToken ct) =>
        Ok(await _svc.GetByPlanningAsync(planningId, ct));

    [HttpPost("replacement")]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> CreateReplacement([FromBody] CreateGuardShiftReplacementDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateReplacementAsync(dto, ct);
        return Ok(created);
    }

    [HttpPost("{id:int}/approve")]
    [RequirePermission("GUARDS.APPROVE")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveGuardShiftChangeDto dto, CancellationToken ct) =>
        Ok(await _svc.ApproveAsync(id, dto, ct));

    [HttpPost("{id:int}/reject")]
    [RequirePermission("GUARDS.APPROVE")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectGuardShiftChangeDto dto, CancellationToken ct) =>
        Ok(await _svc.RejectAsync(id, dto, ct));
}
