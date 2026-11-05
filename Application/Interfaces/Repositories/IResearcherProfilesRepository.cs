using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IResearcherProfilesRepository
{
    /// <summary>Perfil de la persona con sus IDs (activos e inactivos) o null si no tiene.</summary>
    Task<ResearcherProfile?> GetByPersonIdAsync(int personId, CancellationToken ct);

    Task<bool> OrcidExistsForOtherPersonAsync(string orcid, int personId, CancellationToken ct);

    /// <summary>true si un ID activo del proveedor ya pertenece a un perfil distinto de <paramref name="profileId"/>.</summary>
    Task<bool> ActiveProviderIdOwnedByOtherAsync(int providerTypeId, string externalId, int profileId, CancellationToken ct);

    /// <summary>Nombres y apellidos de la persona en RR.HH. (null si no existe).</summary>
    Task<(string FirstName, string LastName)?> GetPersonNameAsync(int personId, CancellationToken ct);

    Task AddProfileAsync(ResearcherProfile profile, CancellationToken ct);

    Task AddProviderIdAsync(ResearcherProviderId providerId, CancellationToken ct);

    /// <summary>Guarda los cambios de las entidades ya rastreadas (perfil e IDs).</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
