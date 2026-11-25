using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Repositorio de solo lectura para la vista HR.vw_EmployeePositions.</summary>
public interface IVwEmployeePositionRepository
{
    /// <summary>Posiciones vigentes de un empleado.</summary>
    Task<List<VwEmployeePosition>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Posiciones vigentes de varios empleados (la pantalla pide solo los de la página visible).</summary>
    Task<List<VwEmployeePosition>> GetByEmployeesAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct = default);
}
