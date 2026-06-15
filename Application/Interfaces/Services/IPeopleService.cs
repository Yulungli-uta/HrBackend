using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPeopleService : IService<People, int>
{
    /// <summary>Retorna las personas cuyo IdCard coincida con alguna de las identificaciones dadas.</summary>
    Task<List<People>> GetByIdentificationsAsync(IEnumerable<string> identifications, CancellationToken ct);
}
