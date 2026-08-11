using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class AddressesRepository : ServiceAwareEfRepository<Addresses, int>, IAddressesRepository
{
    private readonly DbContext _db;
    public AddressesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<Addresses>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<Addresses>().Where(a => a.PersonId == personId).ToListAsync();
    }
}
