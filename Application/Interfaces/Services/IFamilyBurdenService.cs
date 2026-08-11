using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IFamilyBurdenService : IService<FamilyBurden, int>
{
    Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId);

    Task<(FamilyBurden entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        FamilyBurden entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
