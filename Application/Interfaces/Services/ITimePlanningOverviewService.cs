using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TimePlanning;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Consulta de planificaciones de tiempo (horas extra) y sus empleados para Recursos Humanos.</summary>
public interface ITimePlanningOverviewService
{
    /// <summary>Planificaciones paginadas, limitadas al alcance por departamento del usuario.</summary>
    /// <param name="filter">Filtros y paginación.</param>
    /// <param name="requesterEmployeeId">Empleado que consulta (para resolver su alcance); null = sin restricción.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<PagedResult<TimePlanningOverviewItemDto>> GetPlansAsync(TimePlanningOverviewFilter filter, int? requesterEmployeeId, CancellationToken ct = default);

    /// <summary>Ejecuciones registradas para un empleado de una planificación, limitadas al alcance del usuario.</summary>
    Task<IReadOnlyList<TimePlanningOverviewExecutionDto>> GetPlanEmployeeExecutionsAsync(int planEmployeeId, int? requesterEmployeeId, CancellationToken ct = default);

    /// <summary>Empleados de una planificación, paginados y limitados al alcance del usuario.</summary>
    Task<PagedResult<TimePlanningOverviewEmployeeDto>> GetPlanEmployeesAsync(
        int planId, int page, int pageSize, int? employeeStatusTypeId, string? search, int? requesterEmployeeId, CancellationToken ct = default);
}
