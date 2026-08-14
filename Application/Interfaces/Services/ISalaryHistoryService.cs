using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface ISalaryHistoryService : IService<SalaryHistory, int>
{
    /// <summary>
    /// Crea o actualiza la fila de <c>SalaryHistory</c> ligada a un contrato específico.
    /// Se usa tanto al firmar/vigencia (creación) como al corregir (actualiza la fila existente
    /// de ese contrato en vez de insertar una nueva o tocar la de otro documento).
    /// </summary>
    Task UpsertForContractAsync(
        int contractId, int employeeId, decimal newSalary,
        string changedBy, string? reason, CancellationToken ct);

    /// <summary>
    /// Crea o actualiza la fila de <c>SalaryHistory</c> ligada a una acción de personal específica.
    /// Mismo criterio que <see cref="UpsertForContractAsync"/> pero para el documento "acción de personal".
    /// </summary>
    Task UpsertForActionAsync(
        int actionId, int employeeId, decimal previousSalary, decimal newSalary,
        string changedBy, string? reason, CancellationToken ct);
}
