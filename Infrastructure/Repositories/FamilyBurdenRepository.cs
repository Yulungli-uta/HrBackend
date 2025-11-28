using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class FamilyBurdenRepository : ServiceAwareEfRepository<FamilyBurden, int>, IFamilyBurdenRepository
{
    public FamilyBurdenRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId)
    {
        return await _dbSet.Where(f => f.PersonId == personId).ToListAsync();
    }
}
