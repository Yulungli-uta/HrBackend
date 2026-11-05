using WsUtaSystem.Application.DTOs.ResearcherProfiles;

namespace WsUtaSystem.Application.DTOs.PublicationSync;

/// <summary>Estado de cada artículo en la vista previa de la sincronización.</summary>
public static class PublicationSyncItemStatus
{
    /// <summary>No existe: se importará.</summary>
    public const string New = "NEW";
    /// <summary>Ya está registrada y no hay nada que completar.</summary>
    public const string Existing = "EXISTING";
    /// <summary>Ya está registrada; se le completarán los campos que tiene vacíos.</summary>
    public const string Completable = "COMPLETABLE";
    /// <summary>Coincide con una publicación aprobada o rechazada (congelada): no se toca.</summary>
    public const string Locked = "LOCKED";
    /// <summary>Podría ser una publicación ya registrada: hay que resolverla antes de confirmar.</summary>
    public const string Ambiguous = "AMBIGUOUS";
    /// <summary>El proveedor devolvió datos incompletos: se omite.</summary>
    public const string Error = "ERROR";
}

public static class PublicationSyncResolutionAction
{
    /// <summary>Es la misma que la publicación indicada: se le completan los campos vacíos.</summary>
    public const string Link = "LINK";
    /// <summary>Es distinta: se importa como nueva.</summary>
    public const string Import = "IMPORT";
    public const string Skip = "SKIP";
}

/// <summary>Qué le falta a la persona para poder sincronizar un proveedor.</summary>
public static class PublicationSyncMissing
{
    public const string Orcid = "ORCID";
    public const string ProviderId = "PROVIDER_ID";
    public const string NoAdapter = "NO_ADAPTER";
    public const string NotConfigured = "NOT_CONFIGURED";
}

public sealed record SyncProviderStatusDto(
    string Code,
    string Name,
    string? IdLabel,
    string? ProfileUrlTemplate,
    string? IdPattern,
    bool HasAdapter,
    bool RequiresProviderId,
    bool UsesProfileOrcid,
    bool AllowsMultipleIds,
    IReadOnlyList<ResearcherProviderIdDto> Ids,
    bool Ready,
    IReadOnlyList<string> Missing,
    DateTime? LastSyncedAt,
    string? LastSyncStatus,
    string? LastSyncSummary);

/// <summary>Lo que el diálogo necesita para decidir el paso: ORCID, proveedores y qué falta de cada uno.</summary>
public sealed record PublicationSyncReadinessDto(
    int PersonId,
    bool HasOrcid,
    string? Orcid,
    bool CanEdit,
    IReadOnlyList<SyncProviderStatusDto> Providers);

public sealed record SyncCandidateDto(int PublicationId, string Title, string? Doi, DateOnly? PublicationDate, string ApprovalStatus);

public sealed record PublicationSyncItemDto(
    string ExternalId,
    string Title,
    string? JournalName,
    string? Doi,
    DateOnly? PublicationDate,
    string? TypeLabel,
    IReadOnlyList<string> Affiliations,
    string? RecordUrl,
    string Status,
    int? MatchedPublicationId,
    IReadOnlyList<SyncCandidateDto> Candidates,
    string? Message);

/// <param name="SkippedByType">Publicaciones del proveedor que no se importan por su tipo (libros, cartas, datos…).</param>
public sealed record PublicationSyncPreviewDto(
    Guid PreviewToken,
    int PersonId,
    string ProviderCode,
    bool Available,
    bool IsPartial,
    string? Message,
    int TotalFound,
    int NewCount,
    int ExistingCount,
    int CompletableCount,
    int LockedCount,
    int AmbiguousCount,
    int ErrorCount,
    DateTime? LastSyncedAt,
    IReadOnlyList<PublicationSyncItemDto> Items,
    int SkippedByType = 0);

public sealed record SyncResolutionDto(string ExternalId, string Action, int? PublicationId);

public sealed record PublicationSyncPreviewRequest(string ProviderCode);

public sealed record PublicationSyncConfirmRequest(Guid PreviewToken, string ProviderCode, IReadOnlyList<SyncResolutionDto>? Resolutions);

public sealed record PublicationSyncResultDto(int Created, int Completed, int Skipped, int Locked, int Ambiguous, bool IsPartial);

public sealed record PublicationMassSyncResultDto(
    int PeopleProcessed,
    int PeopleWithError,
    int Created,
    int Completed,
    int AmbiguousSkipped,
    bool StoppedEarly,
    string? StopReason);
