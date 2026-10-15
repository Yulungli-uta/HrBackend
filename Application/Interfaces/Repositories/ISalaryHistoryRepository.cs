using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface ISalaryHistoryRepository : IRepository<SalaryHistory, int>
{
    /// <summary>Fila de historial ligada a un contrato específico (a lo sumo una, por diseño).</summary>
    Task<SalaryHistory?> GetByContractIdAsync(int contractId, CancellationToken ct);

    /// <summary>Fila de historial ligada a una acción de personal específica (a lo sumo una, por diseño).</summary>
    Task<SalaryHistory?> GetByActionIdAsync(int actionId, CancellationToken ct);

    /// <summary>
    /// Todas las filas de historial ligadas a una acción. Por diseño es una, pero la carga
    /// MIGRACION_HISTORICA de 2022 dejó acciones con dos filas (sueldos distintos); al corregir
    /// la acción deben actualizarse todas, no una arbitraria.
    /// </summary>
    Task<List<SalaryHistory>> GetAllByActionIdAsync(int actionId, CancellationToken ct);

    /// <summary>Fila de historial más reciente de un empleado, sin importar si el origen fue un contrato o una acción.</summary>
    Task<SalaryHistory?> GetLatestByEmployeeIdAsync(int employeeId, CancellationToken ct);
}
