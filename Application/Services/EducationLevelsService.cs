using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EducationLevelsService : Service<EducationLevels, int>, IEducationLevelsService
{
    private readonly IEducationLevelsRepository _repository;

    public EducationLevelsService(IEducationLevelsRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<EducationLevels>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
