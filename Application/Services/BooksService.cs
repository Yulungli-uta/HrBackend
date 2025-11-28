using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class BooksService : Service<Books, int>, IBooksService
{
    private readonly IBooksRepository _repository;

    public BooksService(IBooksRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<Books>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
