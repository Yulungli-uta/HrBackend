using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IRefTypesRepository : IRepository<RefTypes, int> {
    Task<IEnumerable<RefTypes>> GetByCategoryAsync(string category, CancellationToken ct);
}
