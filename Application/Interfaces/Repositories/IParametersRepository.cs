using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IParametersRepository : IRepository<Parameters, int> {
    Task<IEnumerable<Parameters>> GetByNameAsync(string name, CancellationToken ct);
}

