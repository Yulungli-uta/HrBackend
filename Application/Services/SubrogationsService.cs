using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class SubrogationsService : Service<Subrogations, int>, ISubrogationsService
{
    public SubrogationsService(ISubrogationsRepository repo) : base(repo) { }
}
