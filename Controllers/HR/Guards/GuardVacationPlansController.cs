using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-vacation-plans")]
public class GuardVacationPlansController : ControllerBase
{
    private readonly IGuardVacationService _svc;
    public GuardVacationPlansController(IGuardVacationService svc) => _svc = svc;

    [HttpGet("by-employee/{employeeId:int}")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetByEmployee(
        int employeeId,
        [FromQuery] int? year,
        CancellationToken ct) =>
        Ok(await _svc.GetPlansByEmployeeAsync(employeeId, year, ct));

    [HttpGet("paged")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? year = null,
        [FromQuery] string? status = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetPlansPagedAsync(page, pageSize, year, status, employeeId, startDate, endDate, ct));
    }

    [HttpGet("{id:int}")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetPlanByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateGuardVacationPlanDto dto, CancellationToken ct)
    {
        var created = await _svc.CreatePlanAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.GuardVacationPlanId }, created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGuardVacationPlanDto dto, CancellationToken ct)
    {
        await _svc.UpdatePlanAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/submit-to-direction")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> SubmitToDirection(int id, [FromBody] SubmitToDirectionDto dto, CancellationToken ct) =>
        Ok(await _svc.SubmitPlanToDirectionAsync(id, dto, ct));

    [HttpPost("{id:int}/approve")]
    [RequirePermission("GUARDS.APPROVE")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveGuardVacationPlanDto dto, CancellationToken ct) =>
        Ok(await _svc.ApprovePlanAsync(id, dto, ct));

    [HttpPost("{id:int}/reject")]
    [RequirePermission("GUARDS.APPROVE")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectGuardVacationPlanDto dto, CancellationToken ct) =>
        Ok(await _svc.RejectPlanAsync(id, dto, ct));
}
