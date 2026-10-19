using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de las preferencias de interfaz (una fila por usuario).</summary>
public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByUserAsync(Guid userId, bool track, CancellationToken ct);
    Task AddAsync(UserPreference preference, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<bool> DeleteByUserAsync(Guid userId, CancellationToken ct);
}
