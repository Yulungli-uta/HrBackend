using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class LanguagesService : Service<Languages, int>, ILanguagesService
{
    private readonly ILanguagesRepository _repository;

    public LanguagesService(ILanguagesRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<Languages>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
