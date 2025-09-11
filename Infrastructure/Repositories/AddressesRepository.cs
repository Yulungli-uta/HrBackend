using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class AddressesRepository : ServiceAwareEfRepository<Addresses, int>, IAddressesRepository
{
    public AddressesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
