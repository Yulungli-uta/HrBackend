namespace WsUtaSystem.Application.DTOs.ResearcherProfiles;

/// <summary>Perfil de investigador de una persona y sus IDs por plataforma.</summary>
/// <param name="Orcid">null si la persona todavía no lo registró (es obligatorio para sincronizar publicaciones).</param>
/// <param name="CanEdit">Quien consulta puede registrar/corregir el ORCID y los IDs (la propia persona o un Administrador).</param>
public sealed record ResearcherProfileDto(
    int PersonId,
    string? Orcid,
    DateTime? OrcidEnteredAt,
    IReadOnlyList<ResearcherProviderIdDto> ProviderIds,
    bool CanEdit);

public sealed record ResearcherProviderIdDto(
    int Id,
    int ProviderTypeId,
    string ProviderCode,
    string ExternalId,
    DateTime? VerifiedAt,
    DateTime? LastSyncedAt,
    string? LastSyncStatus,
    string? LastSyncSummary);

/// <param name="AcknowledgeNameMismatch">La persona que registra confirma que el ORCID es el correcto aunque el nombre difiera.</param>
public sealed record SetOrcidRequest(string Orcid, bool AcknowledgeNameMismatch = false);

/// <summary>
/// Resultado de registrar el ORCID. Si el nombre que publica ORCID no coincide con el de la persona, no se guarda y se pide
/// confirmación (<paramref name="RequiresConfirmation"/>) mostrando <paramref name="OrcidName"/>.
/// </summary>
/// <param name="Saved">true si el ORCID quedó guardado.</param>
/// <param name="OrcidName">Nombre que publica el registro de ORCID (para que la persona confirme).</param>
/// <param name="Warning">Aviso cuando se guardó sin poder comprobar el nombre (ORCID sin nombre público o sin respuesta).</param>
public sealed record SetOrcidResultDto(
    bool Saved,
    bool RequiresConfirmation,
    string? OrcidName,
    string? Warning,
    ResearcherProfileDto? Profile);

public sealed record AddProviderIdRequest(string ProviderCode, string ExternalId);
