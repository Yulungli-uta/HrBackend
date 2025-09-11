using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class TimeRecoveryPlansRepository : ServiceAwareEfRepository<TimeRecoveryPlans, int>, ITimeRecoveryPlansRepository
{
    public TimeRecoveryPlansRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
