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
        int actionId, int employeeId, decimal? previousSalary, decimal newSalary,
        string changedBy, string? reason, CancellationToken ct);

    /// <summary>
    /// Resuelve el RMU de referencia para prellenar los campos "RMU Origen"/"RMU Destino" del
    /// formulario de Acciones de Personal. Prioridad: (1) último <c>SalaryHistory</c> real del
    /// empleado — el dato individual auténtico — si <paramref name="employeeId"/> tiene historial;
    /// (2) <c>tbl_jobs.ReferenceSalary</c> del cargo (propio o, si no se especifica, el cargo actual
    /// del empleado) — referencial por cargo, no depende de grupo ocupacional; (3)
    /// <c>OccupationalGroup.Rmu</c> del grupo del cargo, como último respaldo. Null si ninguna
    /// fuente tiene dato (caso legítimo, no se inventa un valor).
    /// </summary>
    /// <param name="employeeId">Empleado para resolver su RMU individual real. Null si es un cargo prospectivo (ej. destino de una acción).</param>
    /// <param name="jobId">Cargo para el respaldo referencial. Si es null y se pasó employeeId, se resuelve el cargo actual de ese empleado.</param>
    Task<decimal?> GetReferenceRmuAsync(int? employeeId, int? jobId, CancellationToken ct = default);
}
