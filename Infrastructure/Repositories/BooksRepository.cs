using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class BooksRepository : ServiceAwareEfRepository<Books, int>, IBooksRepository
{
    public BooksRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Books>> GetByPersonIdAsync(int personId)
    {
        return await _dbSet.Where(b => b.PersonId == personId).ToListAsync();
    }

}