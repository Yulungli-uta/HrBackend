using WsUtaSystem.Application.DTOs.EmployeePositions;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Consulta de las posiciones vigentes de los empleados (doble régimen, autoridad y temporales).</summary>
public interface IVwEmployeePositionService
{
    /// <summary>Posiciones vigentes de un empleado, la principal primero.</summary>
    Task<List<EmployeePositionDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Posiciones vigentes de hasta <see cref="MaxEmployees"/> empleados.</summary>
    Task<List<EmployeePositionDto>> GetByEmployeesAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct = default);

    /// <summary>Máximo de empleados por consulta (una página de la lista).</summary>
    const int MaxEmployees = 200;
}
