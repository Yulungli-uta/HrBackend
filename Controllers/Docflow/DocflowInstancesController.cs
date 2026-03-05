// File: Controllers/Docflow/DocflowInstancesController.cs
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Docflow;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Services;

namespace WsUtaSystem.Controllers.Docflow;

[ApiController]
[Route("instances")]
public sealed class DocflowInstancesController : ControllerBase
{
    private readonly IDocflowService _svc;
    public DocflowInstancesController(IDocflowService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? processId = null,
        [FromQuery] string? q = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => Ok(await _svc.SearchInstancesAsync(page, pageSize, status, processId, q, from, to, ct));

   
    [HttpPost]
    [ProducesResponseType(typeof(InstanceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateInstanceRequest req, CancellationToken ct)
        => Ok(await _svc.CreateInstanceAsync(req, ct));

    [HttpGet("{instanceId:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid instanceId, CancellationToken ct)
        => Ok(await _svc.GetInstanceAsync(instanceId, ct));

    [HttpGet("{instanceId:guid}/documents")]
    public async Task<IActionResult> GetDocuments([FromRoute] Guid instanceId, CancellationToken ct)
        => Ok(await _svc.GetInstanceDocumentsAsync(instanceId, ct));

    [HttpPost("{instanceId:guid}/documents")]
    public async Task<IActionResult> CreateDocument([FromRoute] Guid instanceId, [FromBody] CreateDocumentRequest req, CancellationToken ct)
        => Ok(await _svc.CreateDocumentAsync(instanceId, req, ct));

    [HttpPost("{instanceId:guid}/movements")]
    public async Task<IActionResult> Move([FromRoute] Guid instanceId, [FromBody] CreateMovementRequest req, CancellationToken ct)
    {
        await _svc.CreateMovementAsync(instanceId, req, ct);
        return Ok(new { ok = true });
    }
}