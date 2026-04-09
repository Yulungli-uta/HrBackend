using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPermissionsService : IService<Permissions, int> {

    Task<Permissions> CreateWithBalanceCheckAsync(Permissions entity, CancellationToken ct);    
    Task<Permissions> UpdateBalanceAffectAsync(int id, Permissions entity, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct);       
    Task<IEnumerable<Permissions>> GetByImmediateBossIdNonMedical(int employeeId, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetPendingMedicalPermissions(CancellationToken ct);
}

