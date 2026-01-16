using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class TimeBalancesRepository : ServiceAwareEfRepository<TimeBalances, int>, ITimeBalancesRepository
    {
        public TimeBalancesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
    }
}
