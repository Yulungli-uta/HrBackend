using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class OvertimeService : Service<Overtime, int>, IOvertimeService
{
    public OvertimeService(IOvertimeRepository repo) : base(repo) { }
}
