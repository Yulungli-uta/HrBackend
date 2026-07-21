using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.TramiteRequirements;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Parametrización de requisitos documentales por trámite (checklist de obligatoriedad).
/// Catálogo global: cualquier usuario con el permiso de acción correspondiente
/// (TRAMITE_REQUIREMENTS.*) puede consultar o mantener los requisitos de cualquier
/// módulo/trámite, no está limitado por persona. Ruta base: /api/v1/rh/tramite-requirements
/// </summary>
[ApiController]
[Route("tramite-requirements")]
[Produces("application/json")]
public sealed class TramiteRequirementsController : ControllerBase
{
    private readonly ITramiteRequirementsService _svc;
    private readonly ICurrentUserService _currentUser;

    public TramiteRequirementsController(ITramiteRequirementsService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    /// <summary>Módulos (trámites) disponibles para parametrizar requisitos documentales.</summary>
    [HttpGet("accessible-modules")]
    [RequirePermission("TRAMITE_REQUIREMENTS.READ")]
    [ProducesResponseType(typeof(List<AccessibleModuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccessibleModules(CancellationToken ct) =>
        Ok(await _svc.GetAccessibleModulesAsync(ct));

    /// <summary>Requisitos configurados para un módulo.</summary>
    [HttpGet("module/{moduleTypeId:int}")]
    [RequirePermission("TRAMITE_REQUIREMENTS.READ")]
    [ProducesResponseType(typeof(List<TramiteRequirementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByModule([FromRoute] int moduleTypeId, CancellationToken ct) =>
        Ok(await _svc.GetByModuleAsync(moduleTypeId, ct));

    /// <summary>
    /// Requisitos aplicables a un módulo/tipo específico (p.ej. un tipo de contrato), para que
    /// cualquiera completando ese trámite sepa qué documentos debe adjuntar. Lectura abierta:
    /// no requiere el permiso de administración del catálogo (TRAMITE_REQUIREMENTS.READ).
    /// </summary>
    [HttpGet("applicable")]
    [ProducesResponseType(typeof(List<TramiteRequirementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicable(
        [FromQuery] int moduleTypeId, [FromQuery] int? specificTypeId, CancellationToken ct) =>
        Ok(await _svc.GetApplicableAsync(moduleTypeId, specificTypeId, ct));

    [HttpPost]
    [RequirePermission("TRAMITE_REQUIREMENTS.CREATE")]
    [ProducesResponseType(typeof(TramiteRequirementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] TramiteRequirementCreateDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(dto, _currentUser.EmployeeId, ct);
            return CreatedAtAction(nameof(GetByModule), new { moduleTypeId = created.ModuleTypeId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error al crear el requisito.", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission("TRAMITE_REQUIREMENTS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TramiteRequirementUpdateDto dto, CancellationToken ct)
    {
        try
        {
            await _svc.UpdateAsync(id, dto, _currentUser.EmployeeId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("TRAMITE_REQUIREMENTS.DELETE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
