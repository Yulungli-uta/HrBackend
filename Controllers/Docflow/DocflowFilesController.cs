// File: Controllers/Docflow/DocflowFilesController.cs
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.Docflow;

[ApiController]
[Route("versions")]
public sealed class DocflowFilesController : ControllerBase
{
    private readonly IDocflowService _svc;
    public DocflowFilesController(IDocflowService svc) => _svc = svc;

    [HttpGet("{versionId:guid}/download")]
    public async Task<IActionResult> Download([FromRoute] Guid versionId, CancellationToken ct)
    {
        var r = await _svc.DownloadVersionAsync(versionId, ct);
        return File(r.FileBytes, r.ContentType, r.FileName);
    }
}