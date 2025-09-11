using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class CantonsRepository : ServiceAwareEfRepository<Cantons, string>, ICantonsRepository
{
    public CantonsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
