// File: Controllers/Docflow/DocflowDocumentsController.cs
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.Docflow;

[ApiController]
[Route("documents")]
public sealed class DocflowDocumentsController : ControllerBase
{
    private readonly IDocflowService _svc;
    public DocflowDocumentsController(IDocflowService svc) => _svc = svc;

    [HttpGet("{documentId:guid}/versions")]
    public async Task<IActionResult> GetVersions([FromRoute] Guid documentId, CancellationToken ct)
        => Ok(await _svc.GetDocumentVersionsAsync(documentId, ct));

    [HttpPost("{documentId:guid}/versions")]
    public async Task<IActionResult> Upload([FromRoute] Guid documentId, IFormFile file, CancellationToken ct)
        => Ok(await _svc.UploadDocumentVersionAsync(documentId, file, ct));
}