using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class CantonsService : Service<Cantons, string>, ICantonsService
{
    public CantonsService(ICantonsRepository repo) : base(repo) { }
}
