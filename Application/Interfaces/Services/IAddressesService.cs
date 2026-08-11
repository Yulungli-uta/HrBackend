using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IAddressesService : IService<Addresses, int>
{
    Task<IEnumerable<Addresses>> GetByPersonIdAsync(int personId);
}
