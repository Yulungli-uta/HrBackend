using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("vw-departments")]
public class VwDepartmentWithTypeController : ControllerBase
{
    private readonly IVwDepartmentWithTypeService _svc;

    public VwDepartmentWithTypeController(IVwDepartmentWithTypeService svc) => _svc = svc;

    /// <summary>Lista todos los departamentos con su tipo.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    /// <summary>Lista únicamente los departamentos activos.</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct) =>
        Ok(await _svc.GetActiveAsync(ct));

    /// <summary>Lista departamentos filtrados por tipo.</summary>
    /// <param name="typeId">ID del tipo de departamento.</param>
    [HttpGet("by-type/{typeId:int}")]
    public async Task<IActionResult> GetByType([FromRoute] int typeId, CancellationToken ct) =>
        Ok(await _svc.GetByTypeAsync(typeId, ct));

    /// <summary>Lista departamentos filtrados por ámbito.</summary>
    /// <param name="scopeId">ID del ámbito del departamento.</param>
    [HttpGet("by-scope/{scopeId:int}")]
    public async Task<IActionResult> GetByScope([FromRoute] int scopeId, CancellationToken ct) =>
        Ok(await _svc.GetByScopeAsync(scopeId, ct));

    /// <summary>Obtiene un departamento por su ID.</summary>
    /// <param name="id">ID del departamento.</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
