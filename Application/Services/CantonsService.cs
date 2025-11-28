using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class CantonsService : Service<Cantons, string>, ICantonsService
{
    private readonly ICantonsRepository _repository;

    public CantonsService(ICantonsRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<Cantons>> GetByProvinceIdAsync(string provinceId)
    {
        return await _repository.GetByProvinceIdAsync(provinceId);
    }
}
