using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Publications;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Revisión de publicaciones por la Dirección de Investigación. Es el único camino que cambia el estado de una publicación
/// (SIN_REVISION ↔ APROBADA / RECHAZADA); aprobar la congela. Las acciones masivas procesan cada publicación por separado:
/// las que no se pueden procesar se informan y no detienen a las demás.
/// </summary>
public interface IPublicationReviewService
{
    Task<PagedResult<PublicationReviewItemDto>> GetPageAsync(PublicationReviewFilterDto filter, CancellationToken ct);

    Task<PublicationReviewSummaryDto> GetSummaryAsync(CancellationToken ct);

    /// <summary>SIN_REVISION → APROBADA (queda congelada).</summary>
    Task<PublicationReviewResultDto> ApproveAsync(IReadOnlyList<int> ids, int? actorEmployeeId, CancellationToken ct);

    /// <summary>SIN_REVISION → RECHAZADA. El motivo es obligatorio.</summary>
    Task<PublicationReviewResultDto> RejectAsync(IReadOnlyList<int> ids, string? notes, int? actorEmployeeId, CancellationToken ct);

    /// <summary>APROBADA → SIN_REVISION (descongela). El motivo es obligatorio y queda registrado.</summary>
    Task<PublicationReviewResultDto> ReopenAsync(IReadOnlyList<int> ids, string? notes, int? actorEmployeeId, CancellationToken ct);

    /// <summary>RECHAZADA → SIN_REVISION.</summary>
    Task<PublicationReviewResultDto> RestoreAsync(IReadOnlyList<int> ids, int? actorEmployeeId, CancellationToken ct);
}
