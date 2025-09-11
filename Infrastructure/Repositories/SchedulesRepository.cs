using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class SchedulesRepository : ServiceAwareEfRepository<Schedules, int>, ISchedulesRepository
{
    public SchedulesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
