using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPublicationsService : IService<Publications, int>
{
    Task<IEnumerable<Publications>> GetByPersonIdAsync(int personId);

    /// <summary>
    /// Publicaciones que cuentan en el CV en PDF y en la promoción docente: aprobadas, y sin revisión de origen histórico o
    /// manual. Lo sincronizado sin aprobar y lo rechazado quedan fuera (ver <c>PublicationRules.CountsForCv</c>).
    /// </summary>
    Task<IReadOnlyList<Publications>> GetCountableByPersonIdAsync(int personId, CancellationToken ct);

    Task<(Publications entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        Publications entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
