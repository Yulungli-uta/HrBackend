using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IEmployeeLaborRegimeRepository : IRepository<EmployeeLaborRegime, int>
{
    /// <summary>Filas activas de régimen laboral de un empleado.</summary>
    Task<List<EmployeeLaborRegime>> GetActiveByEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Copia el cargo y/o la unidad editados en la ficha del empleado a su fila de régimen principal vigente, para que la lista
    /// (HR.vw_EmployeePositions) y SIIES muestren lo que se guardó. Un valor nulo no cambia esa columna. Devuelve las filas modificadas.
    /// </summary>
    Task<int> ApplyPositionToCurrentPrincipalAsync(int employeeId, int? jobId, int? departmentId, DateOnly today, CancellationToken ct = default);

    /// <summary>Todas las filas (activas e históricas) de un empleado, más recientes primero.</summary>
    Task<List<EmployeeLaborRegime>> GetAllByEmployeeAsync(int employeeId, CancellationToken ct = default);

    Task<string?> GetRegimeNameAsync(int laborRegimeId, CancellationToken ct = default);

    /// <summary>
    /// Unión de LaborRegimeId activos entre todos los empleados dados (un empleado puede
    /// tener más de un régimen activo simultáneo). Usado para filtrar horarios compatibles
    /// al planificar un cambio de horario para uno o varios colaboradores.
    /// </summary>
    Task<List<int>> GetActiveRegimeIdsByEmployeeIdsAsync(List<int> employeeIds, CancellationToken ct = default);
}
