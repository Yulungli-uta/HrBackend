using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class AddressesService : Service<Addresses, int>, IAddressesService
{
    private readonly IAddressesRepository _repository;

    public AddressesService(IAddressesRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<Addresses>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
