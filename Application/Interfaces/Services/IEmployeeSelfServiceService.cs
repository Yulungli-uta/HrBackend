using WsUtaSystem.Application.DTOs.EmployeeSelfService;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Agregador de lectura para el panel de autoservicio del empleado. Compone servicios ya
/// existentes (Permisos, Vacaciones, TimeBalances, Certificados, Solicitudes internas) —
/// no introduce tablas ni lógica de negocio nueva, solo consulta y agrupa.
/// </summary>
public interface IEmployeeSelfServiceService
{
    Task<EmployeeSelfServiceProfileDto> GetProfileAsync(int employeeId, CancellationToken ct = default);

    Task<EmployeeSelfServiceSummaryDto> GetSummaryAsync(int employeeId, CancellationToken ct = default);

    Task<IReadOnlyList<EmployeeSelfServiceHistoryEntryDto>> GetHistoryAsync(int employeeId, CancellationToken ct = default);
}
