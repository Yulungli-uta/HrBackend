using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.AcademicLadder;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>Gestión del escalafón docente secuencial (LOES).</summary>
[ApiController]
[Route("academic-ladder")]
public class AcademicLadderController : ControllerBase
{
    private readonly IAcademicLadderService _svc;
    public AcademicLadderController(IAcademicLadderService svc) => _svc = svc;

    /// <summary>Lista todos los escalafones ordenados por secuencia.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    /// <summary>Escalafón por Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Devuelve el escalafón al que puede postular desde el indicado.</summary>
    [HttpGet("{id:int}/next")]
    public async Task<IActionResult> GetNext(int id, CancellationToken ct)
    {
        var result = await _svc.GetNextAsync(id, ct);
        return result is null ? NoContent() : Ok(result);
    }

    /// <summary>Crea un nuevo escalafón.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    public async Task<IActionResult> Create([FromBody] AcademicLadderCreateDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.LadderId }, created);
    }

    /// <summary>Actualiza un escalafón existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Update(int id, [FromBody] AcademicLadderUpdateDto dto, CancellationToken ct)
    {
        var updated = await _svc.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }
}
