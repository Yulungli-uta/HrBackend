using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class CatastrophicIllnessesRepository : ServiceAwareEfRepository<CatastrophicIllnesses, int>, ICatastrophicIllnessesRepository
{
    private readonly DbContext _db;
    public CatastrophicIllnessesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<CatastrophicIllnesses>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<CatastrophicIllnesses>().Where(e => e.PersonId == personId).ToListAsync();
    }
}
