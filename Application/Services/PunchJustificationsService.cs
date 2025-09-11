using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PunchJustificationsService : Service<PunchJustifications, int>, IPunchJustificationsService
{
    public PunchJustificationsService(IPunchJustificationsRepository repo) : base(repo) { }
}
