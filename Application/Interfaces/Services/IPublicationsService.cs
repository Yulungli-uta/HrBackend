using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPublicationsService : IService<Publications, int>
{
    Task<IEnumerable<Publications>> GetByPersonIdAsync(int personId);

    Task<(Publications entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        Publications entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
