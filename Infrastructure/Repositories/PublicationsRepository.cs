using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PublicationsRepository : ServiceAwareEfRepository<Publications, int>, IPublicationsRepository
{
    private readonly DbContext _db;
    public PublicationsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<Publications>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<Publications>().Where(p => p.PersonId == personId).ToListAsync();
    }
}
