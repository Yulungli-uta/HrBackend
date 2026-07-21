using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("rotation-patterns")]
public class RotationPatternsController : ControllerBase
{
    private readonly IRotationPatternService _svc;
    public RotationPatternsController(IRotationPatternService svc) => _svc = svc;

    [HttpGet]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpGet("paged")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetPagedAsync(page, pageSize, search, isActive, ct));
    }

    [HttpGet("{id:int}")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateRotationPatternDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.PatternId }, created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRotationPatternDto dto, CancellationToken ct)
    {
        await _svc.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/details")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> SetDetails(int id, [FromBody] UpsertRotationPatternDetailsDto dto, CancellationToken ct) =>
        Ok(await _svc.SetDetailsAsync(id, dto, ct));
}
