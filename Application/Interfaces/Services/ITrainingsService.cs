using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface ITrainingsService : IService<Trainings, int>
{
    Task<IEnumerable<Trainings>> GetByPersonIdAsync(int personId);

    Task<(Trainings entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        Trainings entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
