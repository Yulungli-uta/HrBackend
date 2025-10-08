using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class ActivityService : Service<Activity, int>, IActivityService
{
    public ActivityService(IActivityRepository repo) : base(repo) { }
}
