namespace WsUtaSystem.Application.DTOs.Publications;

/// <summary>Filtros de la pantalla de revisión de la Dirección de Investigación.</summary>
/// <param name="Status">SIN_REVISION (por defecto), APROBADA, RECHAZADA o ALL.</param>
/// <param name="SyncedOnly">Solo lo sincronizado de proveedores que exigen aprobación (por defecto true).</param>
public sealed record PublicationReviewFilterDto(
    string? Status = "SIN_REVISION",
    bool SyncedOnly = true,
    string? ProviderCode = null,
    string? Search = null,
    int? Year = null,
    DateOnly? SyncedFrom = null,
    DateOnly? SyncedTo = null,
    int Page = 1,
    int PageSize = 25);

public sealed record PublicationReviewItemDto(
    int PublicationId,
    int PersonId,
    string PersonName,
    string IdCard,
    string Title,
    string? JournalName,
    string? Doi,
    string? Link,
    DateOnly? PublicationDate,
    int SourceProviderTypeId,
    string ProviderCode,
    string ProviderName,
    string? ExternalId,
    string ApprovalStatus,
    DateTime? LastSyncedAt,
    DateTime? ApprovedAt,
    string? ReviewNotes);

/// <summary>Conteos para las tarjetas de la pantalla (mismo alcance que los filtros de proveedor y sincronización).</summary>
public sealed record PublicationReviewSummaryDto(
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int PeopleWithPending);

public sealed record PublicationReviewActionRequest(IReadOnlyList<int> Ids, string? Notes);

public sealed record PublicationReviewFailureDto(int Id, string Reason);

/// <summary>Resultado de una acción (individual o masiva): las que fallan no detienen a las demás.</summary>
public sealed record PublicationReviewResultDto(int Processed, IReadOnlyList<PublicationReviewFailureDto> Failed);
