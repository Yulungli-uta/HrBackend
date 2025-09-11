using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class BooksRepository : ServiceAwareEfRepository<Books, int>, IBooksRepository
{
    public BooksRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
