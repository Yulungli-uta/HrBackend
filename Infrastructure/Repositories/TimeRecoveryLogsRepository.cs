using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class TimeRecoveryLogsRepository : ServiceAwareEfRepository<TimeRecoveryLogs, int>, ITimeRecoveryLogsRepository
{
    public TimeRecoveryLogsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
