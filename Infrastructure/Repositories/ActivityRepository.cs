using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class ActivityRepository : ServiceAwareEfRepository<Activity, int>, IActivityRepository
{
    public ActivityRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
