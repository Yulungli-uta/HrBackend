using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.ResearchProjects;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>Gestión de proyectos de investigación (rol R_PROYECTOS) y descarga de sus resoluciones.</summary>
[ApiController]
[Route("research-projects")]
public sealed class ResearchProjectsController : ControllerBase
{
    // Quienes pueden abrir cualquier documento (el resto solo los de proyectos donde participa).
    private static readonly string[] DocumentRoles = { "Administrador", "R_PROYECTOS", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IResearchProjectsService _service;
    private readonly ICurrentUserService _currentUser;

    public ResearchProjectsController(IResearchProjectsService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Lista paginada de proyectos. Filtros opcionales: nombre o número de resolución (<c>search</c>) y rango de fechas
    /// (<c>from</c> / <c>to</c>, "yyyy-MM-dd"): proyectos cuyo período se cruza con el rango.
    /// </summary>
    [HttpGet]
    [RequirePermission("RESEARCH_PROJECTS.READ", Strict = true)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _service.SearchAsync(search, from, to, page, pageSize, ct));

    /// <summary>Proyecto completo con todos sus participantes.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("RESEARCH_PROJECTS.READ", Strict = true)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var project = await _service.GetAsync(id, ct);
        return project is null ? NotFound() : Ok(project);
    }

    /// <summary>Registra un proyecto (multipart: datos en JSON + 3 resoluciones en PDF).</summary>
    [HttpPost]
    [RequirePermission("RESEARCH_PROJECTS.CREATE", Strict = true)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] ResearchProjectFormDto form, CancellationToken ct)
    {
        var created = await _service.SaveAsync(null, form.Data, form.ApprovalFile, form.ExtensionFile, form.CompletionFile, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Edita un proyecto; los archivos enviados reemplazan a los anteriores.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("RESEARCH_PROJECTS.UPDATE", Strict = true)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromForm] ResearchProjectFormDto form, CancellationToken ct)
        => Ok(await _service.SaveAsync(id, form.Data, form.ApprovalFile, form.ExtensionFile, form.CompletionFile, ct));

    /// <summary>Elimina (lógicamente) un proyecto y sus participaciones.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("RESEARCH_PROJECTS.DELETE", Strict = true)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>Búsqueda de personas (mínimo 3 caracteres) para elegir participantes internos.</summary>
    [HttpGet("people-search")]
    [RequirePermission("RESEARCH_PROJECTS.READ", Strict = true)]
    public async Task<IActionResult> SearchPeople([FromQuery] string? term, CancellationToken ct)
        => Ok(await _service.SearchPeopleAsync(term, ct));

    /// <summary>Descarga una resolución: roles de gestión/RRHH, o quien participa en el proyecto.</summary>
    [HttpGet("documents/{fileGuid:guid}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> DownloadDocument([FromRoute] Guid fileGuid, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();

        var privileged = DocumentRoles.Any(User.IsInRole);
        var myPersonId = privileged ? null : await _currentUser.GetPersonIdAsync(ct);

        var result = await _service.GetDocumentAsync(fileGuid, privileged, myPersonId, ct);
        if (!result.Found) return NotFound();
        if (!result.Allowed)
        {
            return StatusCode(403, new
            {
                status = "error",
                error = new { code = "FORBIDDEN", message = "No tiene acceso a este documento.", traceId = HttpContext.TraceIdentifier },
            });
        }

        return File(result.Bytes!, result.ContentType ?? "application/pdf", result.FileName);
    }
}
