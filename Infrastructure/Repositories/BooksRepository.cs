using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class BooksRepository : ServiceAwareEfRepository<Books, int>, IBooksRepository
{
    private readonly DbContext _db;
    public BooksRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<Books>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<Books>().Where(b => b.PersonId == personId).ToListAsync();
    }

}