using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PermissionTypesService : Service<PermissionTypes, int>, IPermissionTypesService
{
    public PermissionTypesService(IPermissionTypesRepository repo) : base(repo) { }
}
