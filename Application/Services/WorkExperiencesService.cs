using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class WorkExperiencesService : Service<WorkExperiences, int>, IWorkExperiencesService
{
    private readonly IWorkExperiencesRepository _repository;

    public WorkExperiencesService(IWorkExperiencesRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<WorkExperiences>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
