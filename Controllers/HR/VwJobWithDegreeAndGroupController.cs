using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("vw-jobs")]
public class VwJobWithDegreeAndGroupController : ControllerBase
{
    private readonly IVwJobWithDegreeAndGroupService _svc;

    public VwJobWithDegreeAndGroupController(IVwJobWithDegreeAndGroupService svc) => _svc = svc;

    /// <summary>Lista todos los cargos con su título académico y grupo ocupacional.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    /// <summary>Lista cargos filtrados por grupo ocupacional.</summary>
    /// <param name="groupId">ID del grupo ocupacional.</param>
    [HttpGet("by-group/{groupId:int}")]
    public async Task<IActionResult> GetByGroup([FromRoute] int groupId, CancellationToken ct) =>
        Ok(await _svc.GetByGroupAsync(groupId, ct));

    /// <summary>Lista cargos cuyos títulos académicos están activos.</summary>
    [HttpGet("active-degree")]
    public async Task<IActionResult> GetWithActiveDegree(CancellationToken ct) =>
        Ok(await _svc.GetWithActiveDegreeAsync(ct));

    /// <summary>Obtiene un cargo por su ID.</summary>
    /// <param name="id">ID del cargo.</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
