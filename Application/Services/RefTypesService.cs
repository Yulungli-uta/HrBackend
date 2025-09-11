using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class RefTypesService : Service<RefTypes, int>, IRefTypesService
{
    private readonly IRefTypesRepository _repository;
    public RefTypesService(IRefTypesRepository repo) : base(repo) {

        _repository = repo;
    }

    public async Task<IEnumerable<RefTypes>> GetByCategoryAsync(string category, CancellationToken ct)
    {
        return await _repository.GetByCategoryAsync(category, ct);
    }

}
