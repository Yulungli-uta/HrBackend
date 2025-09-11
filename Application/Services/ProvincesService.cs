using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class ProvincesService : Service<Provinces, string>, IProvincesService
{
    public ProvincesService(IProvincesRepository repo) : base(repo) { }
}
