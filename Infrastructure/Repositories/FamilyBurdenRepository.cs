using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class FamilyBurdenRepository : ServiceAwareEfRepository<FamilyBurden, int>, IFamilyBurdenRepository
{
    private readonly DbContext _db;
    public FamilyBurdenRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<FamilyBurden>().Where(f => f.PersonId == personId).ToListAsync();
    }
}
