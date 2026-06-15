using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.TeacherStructure;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

/// <summary>Gestión de la estructura docente (categoría, nivel, dedicación y RMU por empleado).</summary>
[ApiController]
[Route("teacher-structures")]
public class TeacherStructureController : ControllerBase
{
    private readonly ITeacherStructureService _svc;
    public TeacherStructureController(ITeacherStructureService svc) => _svc = svc;

    /// <summary>Listado paginado con filtros opcionales.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] TeacherStructureFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetPagedAsync(filter, ct));

    /// <summary>Todas las estructuras docentes de un empleado.</summary>
    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId, CancellationToken ct) =>
        Ok(await _svc.GetByEmployeeAsync(employeeId, ct));

    /// <summary>Estructura docente por Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Crea una nueva estructura docente.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TeacherStructureCreateDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.TeacherStructureId }, created);
    }

    /// <summary>Actualiza una estructura docente existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TeacherStructureUpdateDto dto, CancellationToken ct)
    {
        var updated = await _svc.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Inactiva (soft-delete) una estructura docente.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _svc.DeactivateAsync(id, ct);
        return NoContent();
    }
}
