using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPermissionsService : IService<Permissions, int> {
    Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct);
}
