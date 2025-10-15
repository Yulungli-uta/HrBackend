using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IDirectoryParametersRepository : IRepository<DirectoryParameters, int> 
{
    Task<DirectoryParameters?> GetByCodeAsync(string code, CancellationToken ct = default);
}

