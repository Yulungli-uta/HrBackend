// File: Controllers/Docflow/DocflowProcessDynamicFieldsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Docflow;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.Docflow;

[ApiController]
[Route("api/v1/docflow/processes")]
public sealed class DocflowProcessDynamicFieldsController : ControllerBase
{
    private readonly IDocflowService _svc;
    public DocflowProcessDynamicFieldsController(IDocflowService svc) => _svc = svc;

    // Lectura (cualquier usuario autenticado)
    [HttpGet("{processId:int}/dynamic-fields")]
    public async Task<IActionResult> Get([FromRoute] int processId, CancellationToken ct)
        => Ok(await _svc.GetProcessDynamicFieldsAsync(processId, ct));

    // Escritura (ajusta roles/policy según tu sistema)
    [Authorize] // ideal: [Authorize(Roles="ADMIN,DOCFLOW_ADMIN")]
    [HttpPut("{processId:int}/dynamic-fields")]
    public async Task<IActionResult> Update([FromRoute] int processId, [FromBody] UpdateProcessDynamicFieldsRequest req, CancellationToken ct)
    {
        await _svc.UpdateProcessDynamicFieldsAsync(processId, req, ct);
        return Ok(new { ok = true });
    }
}