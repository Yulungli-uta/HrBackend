using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PublicationsService : Service<Publications, int>, IPublicationsService
{
    private readonly IPublicationsRepository _repository;

    public PublicationsService(IPublicationsRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<Publications>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
