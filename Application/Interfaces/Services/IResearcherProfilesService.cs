using WsUtaSystem.Application.DTOs.ResearcherProfiles;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Perfil de investigador: ORCID único por persona e IDs de la persona en cada plataforma de publicaciones científicas.
/// La autorización (quién puede editar) la aplica el controlador; aquí viven las reglas de datos.
/// </summary>
public interface IResearcherProfilesService
{
    /// <summary>Perfil de la persona (sin ORCID si aún no lo registró). <paramref name="canEdit"/> lo decide el llamador.</summary>
    Task<ResearcherProfileDto> GetAsync(int personId, bool canEdit, CancellationToken ct);

    /// <summary>Entidad del perfil con sus IDs (para el servicio de sincronización) o null.</summary>
    Task<ResearcherProfile?> GetEntityAsync(int personId, CancellationToken ct);

    /// <summary>
    /// Registra o corrige el ORCID. Valida formato, dígito verificador y que no pertenezca a otra persona; comprueba en el
    /// registro público de ORCID que exista y que su nombre corresponda al de la persona (si no coincide, pide confirmación en
    /// lugar de guardar). Si ORCID no responde, guarda con una advertencia.
    /// </summary>
    Task<SetOrcidResultDto> SetOrcidAsync(int personId, string orcid, bool acknowledgeNameMismatch, int? actorEmployeeId, CancellationToken ct);

    /// <summary>Agrega (o reactiva) el ID de la persona en un proveedor. Requiere que el ORCID ya esté registrado.</summary>
    Task<ResearcherProviderIdDto> AddProviderIdAsync(int personId, string providerCode, string externalId, CancellationToken ct);

    /// <summary>Desactiva un ID (no se borra: queda el historial de sincronizaciones).</summary>
    Task RemoveProviderIdAsync(int personId, int providerIdId, CancellationToken ct);
}
