using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de la sincronización de publicaciones (EF Core; sin lógica de negocio).</summary>
public interface IPublicationSyncRepository
{
    /// <summary>Todas las publicaciones de la persona (incluidas rechazadas), rastreadas por EF si <paramref name="tracked"/>.</summary>
    Task<List<Publications>> GetPersonPublicationsAsync(int personId, bool tracked, CancellationToken ct);

    /// <summary>
    /// Abre una transacción y toma un candado exclusivo de aplicación sobre <paramref name="resource"/> (sp_getapplock, sin
    /// espera). Devuelve null si otra sincronización ya lo tiene. El candado se libera al confirmar o descartar la transacción.
    /// </summary>
    Task<T?> RunExclusiveAsync<T>(string resource, Func<Task<T>> work, CancellationToken ct) where T : class;

    void Add(Publications entity);

    /// <summary>Descarta lo que el contexto tenga rastreado (el contexto puede reutilizarse en una corrida larga).</summary>
    void ClearTracking();

    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>Personas con empleado activo, ORCID registrado y al menos un ID activo del proveedor.</summary>
    Task<List<MassSyncTarget>> GetMassTargetsAsync(int providerTypeId, bool requiresProviderId, CancellationToken ct);
}

public sealed record MassSyncTarget(int PersonId, string Orcid, IReadOnlyList<string> ProviderIds);
