using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class FamilyBurdenService : Service<FamilyBurden, int>, IFamilyBurdenService
{
    private readonly IFamilyBurdenRepository _repository;

    public FamilyBurdenService(IFamilyBurdenRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
