using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class DirectoryParametersService : Service<DirectoryParameters, int>, IDirectoryParametersService
{
    private readonly IDirectoryParametersRepository _repo;

    public DirectoryParametersService(IDirectoryParametersRepository repo) : base(repo) 
    { 
        _repo = repo;
    }

    public async Task<DirectoryParameters?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _repo.GetByCodeAsync(code, ct);
    }
}

