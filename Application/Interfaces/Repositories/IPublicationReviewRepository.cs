using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Publications;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IPublicationReviewRepository
{
    /// <param name="providerTypeIds">Origenes a incluir (null = todos).</param>
    Task<PagedResult<PublicationReviewRow>> GetPageAsync(
        PublicationReviewFilterDto filter, IReadOnlyCollection<int>? providerTypeIds, CancellationToken ct);

    Task<PublicationReviewCounts> GetCountsAsync(IReadOnlyCollection<int>? providerTypeIds, CancellationToken ct);

    /// <summary>Publicaciones rastreadas por EF, para cambiarles el estado.</summary>
    Task<List<Publications>> GetByIdsTrackedAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

/// <summary>Fila de la lista de revisión (sin resolver aún el nombre del proveedor: lo hace el servicio con el catálogo).</summary>
public sealed record PublicationReviewRow(
    int PublicationId,
    int PersonId,
    string FirstName,
    string LastName,
    string IdCard,
    string Title,
    string? JournalName,
    string? Doi,
    string? Link,
    DateOnly? PublicationDate,
    int SourceProviderTypeId,
    string? ExternalId,
    string ApprovalStatus,
    DateTime? LastSyncedAt,
    DateTime? ApprovedAt,
    string? ReviewNotes);

public sealed record PublicationReviewCounts(int Pending, int Approved, int Rejected, int PeopleWithPending);
