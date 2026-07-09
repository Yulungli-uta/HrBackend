using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IPermissionTypesRepository : IRepository<PermissionTypes, int>
{
    /// <summary>
    /// Retorna los tipos de permiso activos visibles para TODOS los regímenes laborales
    /// activos del empleado (HR.tbl_EmployeeLaborRegime). ContractTypeId == null (todos
    /// los regímenes) siempre se incluye.
    /// </summary>
    Task<IEnumerable<PermissionTypes>> GetAvailableForEmployeeAsync(int employeeId, CancellationToken ct);
}
