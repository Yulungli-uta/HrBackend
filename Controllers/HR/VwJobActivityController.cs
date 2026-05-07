using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("vw-job-activities")]
public class VwJobActivityController : ControllerBase
{
    private readonly IVwJobActivityService _svc;

    public VwJobActivityController(IVwJobActivityService svc) => _svc = svc;

    /// <summary>Lista todas las actividades asignadas a cargos.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    /// <summary>Lista todas las actividades activas (asignación vigente).</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAssignments(CancellationToken ct) =>
        Ok(await _svc.GetActiveAssignmentsAsync(ct));

    /// <summary>Lista actividades de un cargo específico.</summary>
    /// <param name="jobId">ID del cargo.</param>
    [HttpGet("by-job/{jobId:int}")]
    public async Task<IActionResult> GetByJob([FromRoute] int jobId, CancellationToken ct) =>
        Ok(await _svc.GetByJobAsync(jobId, ct));

    /// <summary>Lista solo las actividades activas de un cargo específico.</summary>
    /// <param name="jobId">ID del cargo.</param>
    [HttpGet("by-job/{jobId:int}/active")]
    public async Task<IActionResult> GetActiveByJob([FromRoute] int jobId, CancellationToken ct) =>
        Ok(await _svc.GetActiveByJobAsync(jobId, ct));
}
