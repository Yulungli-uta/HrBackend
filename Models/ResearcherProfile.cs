using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Perfil de investigador de una persona (uno por persona). Guarda el ORCID, único en todo el sistema,
/// que la propia persona registra y que es requisito para sincronizar publicaciones.
/// </summary>
public class ResearcherProfile : IAuditable
{
    public int ResearcherProfileId { get; set; }
    public int PersonId { get; set; }

    /// <summary>
    /// ORCID en formato canónico <c>0000-0000-0000-000X</c> (X en mayúscula). Nulo si la persona solo tiene
    /// registrado el ID de alguna plataforma: sin ORCID no se puede sincronizar publicaciones.
    /// </summary>
    public string? Orcid { get; set; }

    public DateTime? OrcidEnteredAt { get; set; }
    public int? OrcidEnteredBy { get; set; }

    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public ICollection<ResearcherProviderId> ProviderIds { get; set; } = new List<ResearcherProviderId>();
}
