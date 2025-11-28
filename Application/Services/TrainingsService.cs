using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class TrainingsService : Service<Trainings, int>, ITrainingsService
{
    private readonly ITrainingsRepository _repository;

    public TrainingsService(ITrainingsRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<Trainings>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
