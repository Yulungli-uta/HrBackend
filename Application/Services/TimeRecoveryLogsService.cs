using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class TimeRecoveryLogsService : Service<TimeRecoveryLogs, int>, ITimeRecoveryLogsService
{
    public TimeRecoveryLogsService(ITimeRecoveryLogsRepository repo) : base(repo) { }
}
