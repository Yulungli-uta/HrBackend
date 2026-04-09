using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IPermissionsRepository : IRepository<Permissions, int> {
    Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct);
    // Nuevo: permisos del jefe que NO sean médicos
    Task<IEnumerable<Permissions>> GetByImmediateBossIdNonMedical(int immediateBossId, CancellationToken ct);
    // Nuevo: permisos médicos pendientes
    Task<IEnumerable<Permissions>> GetPendingMedicalPermissions(CancellationToken ct);
}
