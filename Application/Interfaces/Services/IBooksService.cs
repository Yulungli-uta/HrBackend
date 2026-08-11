using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IBooksService : IService<Books, int>
{
    Task<IEnumerable<Books>> GetByPersonIdAsync(int personId);

    Task<(Books entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        Books entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
