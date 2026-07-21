using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-service-locations")]
public class GuardServiceLocationsController : ControllerBase
{
    private readonly IGuardServiceLocationService _svc;
    public GuardServiceLocationsController(IGuardServiceLocationService svc) => _svc = svc;

    /// <summary>Retorna árbol jerárquico de ubicaciones.</summary>
    [HttpGet("tree")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetTree(CancellationToken ct) =>
        Ok(await _svc.GetTreeAsync(ct));

    /// <summary>Retorna ubicaciones asignables a guardias.</summary>
    [HttpGet("assignable")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetAssignable(CancellationToken ct) =>
        Ok(await _svc.GetAssignableAsync(ct));

    /// <summary>Retorna una ubicación por ID.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Crea una nueva ubicación de cobertura.</summary>
    [HttpPost]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateGuardServiceLocationDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.LocationId }, created);
    }

    /// <summary>Actualiza una ubicación existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGuardServiceLocationDto dto, CancellationToken ct)
    {
        await _svc.UpdateAsync(id, dto, ct);
        return NoContent();
    }
}
