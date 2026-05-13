using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-rotation-groups")]
public class GuardRotationGroupsController : ControllerBase
{
    private readonly IGuardRotationGroupService _svc;
    public GuardRotationGroupsController(IGuardRotationGroupService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetPagedAsync(page, pageSize, search, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuardRotationGroupDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.GroupId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGuardRotationGroupDto dto, CancellationToken ct)
    {
        await _svc.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/employees")]
    public async Task<IActionResult> GetEmployees(int id, CancellationToken ct) =>
        Ok(await _svc.GetEmployeesAsync(id, ct));

    [HttpPost("{id:int}/employees")]
    public async Task<IActionResult> AssignEmployee(int id, [FromBody] AssignEmployeeToRotationGroupDto dto, CancellationToken ct)
    {
        var result = await _svc.AssignEmployeeAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}/employees")]
    public async Task<IActionResult> RemoveEmployee(int id, [FromBody] RemoveEmployeeFromRotationGroupDto dto, CancellationToken ct)
    {
        await _svc.RemoveEmployeeAsync(id, dto, ct);
        return NoContent();
    }

    [HttpGet("location-summary")]
    public async Task<IActionResult> GetLocationSummary(CancellationToken ct) =>
        Ok(await _svc.GetLocationSummaryAsync(ct));

    [HttpGet("by-location/{locationKey}")]
    public async Task<IActionResult> GetByLocationKey(string locationKey, CancellationToken ct) =>
        Ok(await _svc.GetByLocationKeyAsync(locationKey, ct));
}
