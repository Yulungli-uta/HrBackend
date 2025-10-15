using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IDirectoryParametersService : IService<DirectoryParameters, int> 
{
    Task<DirectoryParameters?> GetByCodeAsync(string code, CancellationToken ct = default);
}

