using Microsoft.AspNetCore.Http;
using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface ICatastrophicIllnessesService : IService<CatastrophicIllnesses, int>
{
    Task<IEnumerable<CatastrophicIllnesses>> GetByPersonIdAsync(int personId);

    Task<(CatastrophicIllnesses entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        CatastrophicIllnesses entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
