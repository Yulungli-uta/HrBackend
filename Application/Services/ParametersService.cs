using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class ParametersService : Service<Parameters, int>, IParametersService
{
    private readonly IParametersRepository _repository;

    public ParametersService(IParametersRepository repo) : base(repo) { 
        _repository = repo;
    }

    public async Task<IEnumerable<Parameters>> GetByNameAsync(string name, CancellationToken ct)
    {
        return await _repository.GetByNameAsync(name, ct);
    }
}

