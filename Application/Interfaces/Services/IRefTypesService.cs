using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Services;
public interface IRefTypesService : IService<RefTypes, int>
{
    Task<IEnumerable<RefTypes>> GetByCategoryAsync(string category, CancellationToken ct);
}
