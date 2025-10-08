using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IPermissionsRepository : IRepository<Permissions, int> {
    Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
}
