using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IWorkExperiencesService : IService<WorkExperiences, int>
{
    Task<IEnumerable<WorkExperiences>> GetByPersonIdAsync(int personId);

    Task<(WorkExperiences entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        WorkExperiences entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
