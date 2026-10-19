using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.UserPreferences;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Preferencias de interfaz del usuario autenticado. El UserId se toma SIEMPRE del token
/// (<see cref="ICurrentUserService"/>); ningún endpoint acepta un identificador del cliente,
/// así un usuario solo puede leer y modificar lo suyo. No otorga ni modifica permisos.
/// </summary>
[ApiController]
[Route("me/preferences")]
public sealed class UserPreferencesController : ControllerBase
{
    private readonly IUserPreferencesService _svc;
    private readonly ICurrentUserService _currentUser;

    public UserPreferencesController(IUserPreferencesService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    private Guid? CurrentUserId => _currentUser.UserId;

    /// <summary>Obtiene las preferencias del usuario (valores por defecto si no tiene).</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        return Ok(await _svc.GetAsync(userId, ct));
    }

    /// <summary>Guarda (reemplaza) las preferencias del usuario.</summary>
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] UserPreferencesDto dto, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        return Ok(await _svc.SaveAsync(userId, dto, ct));
    }

    /// <summary>Restablece los valores originales.</summary>
    [HttpDelete]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        await _svc.ResetAsync(userId, ct);
        return NoContent();
    }
}
