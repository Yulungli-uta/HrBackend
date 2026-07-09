using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class LanguagesRepository : ServiceAwareEfRepository<Languages, int>, ILanguagesRepository
{
    private readonly DbContext _db;
    public LanguagesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<Languages>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<Languages>().Where(l => l.PersonId == personId).ToListAsync();
    }
}
