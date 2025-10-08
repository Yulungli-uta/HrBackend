using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PermissionsService : Service<Permissions, int>, IPermissionsService
{
    public PermissionsService(IPermissionsRepository repo) : base(repo) { }

    public Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
