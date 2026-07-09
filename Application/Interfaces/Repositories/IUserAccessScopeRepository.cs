using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IUserAccessScopeRepository : IRepository<UserAccessScope, int>
{
    /// <summary>Filas activas (no eliminadas, no expiradas) de un empleado para un módulo (por código de ref_Types).</summary>
    Task<List<UserAccessScope>> GetActiveByEmployeeAndModuleAsync(int employeeId, string moduleCode, CancellationToken ct = default);

    /// <summary>Resuelve recursivamente (vía ParentID) los IDs de un departamento + todos sus hijos.</summary>
    Task<List<int>> GetDepartmentTreeIdsAsync(int departmentId, CancellationToken ct = default);

    Task<List<UserAccessScopeHistory>> GetHistoryByEmployeeAsync(int employeeId, CancellationToken ct = default);

    Task AddHistoryAsync(UserAccessScopeHistory history, CancellationToken ct = default);
}
