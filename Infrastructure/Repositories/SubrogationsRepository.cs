using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class SubrogationsRepository : ServiceAwareEfRepository<Subrogations, int>, ISubrogationsRepository
{
    public SubrogationsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
