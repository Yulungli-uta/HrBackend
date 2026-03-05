// File: Controllers/Docflow/DocflowAuditController.cs
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.Docflow;

[ApiController]
[Route("audit")]
public sealed class DocflowAuditController : ControllerBase
{
    private readonly IDocflowService _svc;
    public DocflowAuditController(IDocflowService svc) => _svc = svc;

    [HttpGet("returns")]
    public async Task<IActionResult> Returns(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? processId,
        [FromQuery] int? userId,
        CancellationToken ct)
        => Ok(await _svc.GetReturnsAuditAsync(from, to, processId, userId, ct));
}