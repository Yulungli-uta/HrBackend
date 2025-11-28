using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PublicationsRepository : ServiceAwareEfRepository<Publications, int>, IPublicationsRepository
{
    public PublicationsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Publications>> GetByPersonIdAsync(int personId)
    {
        return await _dbSet.Where(p => p.PersonId == personId).ToListAsync();
    }
}
