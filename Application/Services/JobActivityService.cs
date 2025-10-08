using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class JobActivityService : Service<JobActivity, int>, IJobActivityService
{
    public JobActivityService(IJobActivityRepository repo) : base(repo) { }
}
