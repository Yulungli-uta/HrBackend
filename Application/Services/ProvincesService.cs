using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class ProvincesService : Service<Provinces, string>, IProvincesService
{
    private readonly IProvincesRepository _repository;

    public ProvincesService(IProvincesRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<Provinces>> GetByCountryIdAsync(string countryId)
    {
        return await _repository.GetByCountryIdAsync(countryId);
    }
}
