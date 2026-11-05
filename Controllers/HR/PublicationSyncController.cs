using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.PublicationSync;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Sincronización de publicaciones científicas de una persona con un proveedor externo (Scopus…). Misma autorización que la
/// sincronización de Capacitaciones: la propia persona o un rol elevado, con el permiso de perfil.
/// </summary>
[ApiController]
[Route("cv/publications-sync")]
public class PublicationSyncController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IPublicationSyncService _svc;
    private readonly ICurrentUserService _currentUser;

    public PublicationSyncController(IPublicationSyncService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    /// <summary>Qué le falta a la persona (ORCID, ID de plataforma, configuración) para sincronizar cada proveedor.</summary>
    [HttpGet("person/{personId:int}/readiness")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> Readiness([FromRoute] int personId, CancellationToken ct)
    {
        var denied = await DenyIfNotAllowedAsync(personId, ct);
        if (denied is not null) return denied;

        var canEdit = User.IsInRole("Administrador") || await _currentUser.GetPersonIdAsync(ct) == personId;
        return Ok(await _svc.GetReadinessAsync(personId, canEdit, ct));
    }

    /// <summary>Consulta al proveedor y compara con lo registrado. No guarda nada.</summary>
    [HttpPost("person/{personId:int}/preview")]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    public async Task<IActionResult> Preview([FromRoute] int personId, [FromBody] PublicationSyncPreviewRequest request, CancellationToken ct)
    {
        var denied = await DenyIfNotAllowedAsync(personId, ct);
        if (denied is not null) return denied;

        return Ok(await _svc.PreviewAsync(personId, request.ProviderCode, ct));
    }

    /// <summary>Aplica la vista previa: crea las nuevas (en revisión) y completa los vacíos de las existentes.</summary>
    [HttpPost("person/{personId:int}/confirm")]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    public async Task<IActionResult> Confirm([FromRoute] int personId, [FromBody] PublicationSyncConfirmRequest request, CancellationToken ct)
    {
        var denied = await DenyIfNotAllowedAsync(personId, ct);
        if (denied is not null) return denied;

        return Ok(await _svc.ConfirmAsync(personId, request, ct));
    }

    private async Task<IActionResult?> DenyIfNotAllowedAsync(int personId, CancellationToken ct)
    {
        if (ElevatedRoles.Any(User.IsInRole) || await _currentUser.GetPersonIdAsync(ct) == personId) return null;

        return StatusCode(403, new
        {
            status = "error",
            error = new { code = "FORBIDDEN", message = "No puede sincronizar publicaciones de otra persona.", traceId = HttpContext.TraceIdentifier }
        });
    }
}
