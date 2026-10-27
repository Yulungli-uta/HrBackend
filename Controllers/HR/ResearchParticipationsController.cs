using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>Pestaña "Proyectos de investigación" de la hoja de vida: participación de una persona.</summary>
[ApiController]
[Route("cv/research-participations")]
public sealed class ResearchParticipationsController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_PROYECTOS", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IResearchProjectsService _service;
    private readonly ICurrentUserService _currentUser;

    public ResearchParticipationsController(IResearchProjectsService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>Proyectos donde participa la persona, con solo sus participaciones. Cada persona ve lo suyo.</summary>
    [HttpGet("person/{personId:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetByPerson([FromRoute] int personId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != personId)
        {
            return StatusCode(403, new
            {
                status = "error",
                error = new { code = "FORBIDDEN", message = "No puede consultar la participación en proyectos de otra persona.", traceId = HttpContext.TraceIdentifier },
            });
        }

        return Ok(await _service.GetByPersonAsync(personId, ct));
    }
}
