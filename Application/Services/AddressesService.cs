using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class AddressesService : Service<Addresses, int>, IAddressesService
{
    public AddressesService(IAddressesRepository repo) : base(repo) { }
}
