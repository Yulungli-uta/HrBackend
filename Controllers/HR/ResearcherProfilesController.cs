using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.ResearcherProfiles;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Perfil de investigador: ORCID (único por persona) e IDs de la persona en cada plataforma de publicaciones científicas.
/// Lo registra la propia persona; solo un Administrador puede corregirlo por otra (RR.HH. solo consulta).
/// </summary>
[ApiController]
[Route("cv/researcher-profiles")]
public class ResearcherProfilesController : ControllerBase
{
    private const string AdminRole = "Administrador";
    private static readonly string[] ReaderRoles = { AdminRole, "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA", "R_PROYECTOS" };

    private readonly IResearcherProfilesService _svc;
    private readonly ICurrentUserService _currentUser;

    public ResearcherProfilesController(IResearcherProfilesService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    /// <summary>Perfil de investigador de una persona. La propia persona o un rol de consulta autorizado.</summary>
    [HttpGet("person/{personId:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> Get([FromRoute] int personId, CancellationToken ct)
    {
        var isOwner = await IsOwnerAsync(personId, ct);
        if (!isOwner && !ReaderRoles.Any(User.IsInRole))
            return Forbid403("No puede consultar el perfil de investigador de otra persona.");

        return Ok(await _svc.GetAsync(personId, isOwner || User.IsInRole(AdminRole), ct));
    }

    /// <summary>Registra o corrige el ORCID. Solo la propia persona o un Administrador.</summary>
    [HttpPut("person/{personId:int}/orcid")]
    [RequirePermission("EMPLOYEE_PROFILE.UPDATE")]
    public async Task<IActionResult> SetOrcid([FromRoute] int personId, [FromBody] SetOrcidRequest request, CancellationToken ct)
    {
        if (!await CanEditAsync(personId, ct))
            return Forbid403("Solo la propia persona o un Administrador puede registrar el ORCID.");

        return Ok(await _svc.SetOrcidAsync(personId, request.Orcid, request.AcknowledgeNameMismatch, _currentUser.EmployeeId, ct));
    }

    /// <summary>Agrega el ID de la persona en una plataforma (p. ej. Scopus Author ID).</summary>
    [HttpPost("person/{personId:int}/provider-ids")]
    [RequirePermission("EMPLOYEE_PROFILE.UPDATE")]
    public async Task<IActionResult> AddProviderId([FromRoute] int personId, [FromBody] AddProviderIdRequest request, CancellationToken ct)
    {
        if (!await CanEditAsync(personId, ct))
            return Forbid403("Solo la propia persona o un Administrador puede registrar IDs de plataforma.");

        return Ok(await _svc.AddProviderIdAsync(personId, request.ProviderCode, request.ExternalId, ct));
    }

    /// <summary>Elimina (desactiva) el ID de una plataforma.</summary>
    [HttpDelete("person/{personId:int}/provider-ids/{providerIdId:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.UPDATE")]
    public async Task<IActionResult> RemoveProviderId([FromRoute] int personId, [FromRoute] int providerIdId, CancellationToken ct)
    {
        if (!await CanEditAsync(personId, ct))
            return Forbid403("Solo la propia persona o un Administrador puede eliminar IDs de plataforma.");

        await _svc.RemoveProviderIdAsync(personId, providerIdId, ct);
        return NoContent();
    }

    private async Task<bool> IsOwnerAsync(int personId, CancellationToken ct) =>
        await _currentUser.GetPersonIdAsync(ct) == personId;

    private async Task<bool> CanEditAsync(int personId, CancellationToken ct) =>
        User.IsInRole(AdminRole) || await IsOwnerAsync(personId, ct);

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}
