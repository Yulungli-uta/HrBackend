using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IParametersService : IService<Parameters, int> {

    Task<IEnumerable<Parameters>> GetByNameAsync(string name, CancellationToken ct);
}

