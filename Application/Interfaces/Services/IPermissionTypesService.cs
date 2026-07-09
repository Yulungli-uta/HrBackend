using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;

public interface IPermissionTypesService : IService<PermissionTypes, int>
{
    /// <summary>Retorna los tipos de permiso activos disponibles para todos los regímenes laborales activos del empleado.</summary>
    Task<IEnumerable<PermissionTypes>> GetAvailableForEmployeeAsync(int employeeId, CancellationToken ct);
}
