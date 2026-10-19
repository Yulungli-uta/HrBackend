using WsUtaSystem.Application.DTOs.UserPreferences;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Preferencias de interfaz del usuario autenticado.</summary>
public interface IUserPreferencesService
{
    /// <summary>Devuelve las preferencias del usuario, o valores por defecto (todo nulo) si no hay fila.</summary>
    Task<UserPreferencesResponseDto> GetAsync(Guid userId, CancellationToken ct);

    /// <summary>Valida y reemplaza las preferencias del usuario (crea la fila si no existe).</summary>
    Task<UserPreferencesResponseDto> SaveAsync(Guid userId, UserPreferencesDto dto, CancellationToken ct);

    /// <summary>Restablece los valores originales eliminando la fila del usuario.</summary>
    Task ResetAsync(Guid userId, CancellationToken ct);
}
