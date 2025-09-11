using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PeopleService : Service<People, int>, IPeopleService
{
    public PeopleService(IPeopleRepository repo) : base(repo) { }
}
