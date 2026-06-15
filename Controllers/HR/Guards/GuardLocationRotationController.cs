using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-location-rotation")]
public class GuardLocationRotationController : ControllerBase
{
    private readonly IGuardLocationRotationService _svc;
    public GuardLocationRotationController(IGuardLocationRotationService svc) => _svc = svc;

    // ─── Periodos ─────────────────────────────────────────────────────────────

    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods(CancellationToken ct) =>
        Ok(await _svc.GetPeriodsAsync(ct));

    [HttpGet("periods/paged")]
    public async Task<IActionResult> GetPeriodsPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetPeriodsPagedAsync(page, pageSize, ct));
    }

    [HttpGet("periods/{id:int}")]
    public async Task<IActionResult> GetPeriodById(int id, CancellationToken ct)
    {
        var result = await _svc.GetPeriodByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("periods")]
    public async Task<IActionResult> CreatePeriod([FromBody] CreateGuardLocationRotationPeriodDto dto, CancellationToken ct)
    {
        var created = await _svc.CreatePeriodAsync(dto, ct);
        return CreatedAtAction(nameof(GetPeriodById), new { id = created.LocationRotationPeriodId }, created);
    }

    [HttpPut("periods/{id:int}")]
    public async Task<IActionResult> UpdatePeriod(int id, [FromBody] UpdateGuardLocationRotationPeriodDto dto, CancellationToken ct)
    {
        await _svc.UpdatePeriodAsync(id, dto, ct);
        return NoContent();
    }

    // ─── Asignaciones ─────────────────────────────────────────────────────────

    [HttpGet("periods/{periodId:int}/assignments")]
    public async Task<IActionResult> GetAssignmentsByPeriod(int periodId, CancellationToken ct) =>
        Ok(await _svc.GetAssignmentsByPeriodAsync(periodId, ct));

    [HttpGet("assignments/by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetAssignmentsByEmployee(int employeeId, CancellationToken ct) =>
        Ok(await _svc.GetAssignmentsByEmployeeAsync(employeeId, ct));

    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateGuardLocationRotationAssignmentDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAssignmentAsync(dto, ct));

    [HttpPut("assignments/{id:int}")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateGuardLocationRotationAssignmentDto dto, CancellationToken ct)
    {
        await _svc.UpdateAssignmentAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("assignments/{id:int}")]
    public async Task<IActionResult> DeleteAssignment(int id, CancellationToken ct)
    {
        await _svc.DeleteAssignmentAsync(id, ct);
        return NoContent();
    }
}
