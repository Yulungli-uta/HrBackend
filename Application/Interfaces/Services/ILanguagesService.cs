using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface ILanguagesService : IService<Languages, int>
{
    Task<IEnumerable<Languages>> GetByPersonIdAsync(int personId);

    Task<(Languages entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        Languages entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
