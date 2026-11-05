using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// ID de un investigador en una plataforma de publicaciones científicas (p. ej. Scopus Author ID).
/// El proveedor es un tipo de ref_Types (categoría SCIENTIFIC_PROVIDER). Un ID activo no puede pertenecer a dos personas.
/// </summary>
public class ResearcherProviderId : IAuditable
{
    public int ResearcherProviderIdId { get; set; }
    public int ResearcherProfileId { get; set; }
    public int ProviderTypeId { get; set; }
    public string ExternalId { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    /// <summary>Se llena en la primera importación confirmada (la persona confirmó que los artículos son suyos).</summary>
    public DateTime? VerifiedAt { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    /// <summary>OK, PARCIAL o ERROR (resultado de la última sincronización).</summary>
    public string? LastSyncStatus { get; set; }
    public string? LastSyncSummary { get; set; }

    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public ResearcherProfile? ResearcherProfile { get; set; }
}
