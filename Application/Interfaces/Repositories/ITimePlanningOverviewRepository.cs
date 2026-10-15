using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TimePlanning;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Consultas de solo lectura de planificaciones de tiempo para Recursos Humanos.</summary>
public interface ITimePlanningOverviewRepository
{
    /// <summary>Lista paginada de planificaciones según los filtros indicados.</summary>
    Task<PagedResult<TimePlanningOverviewItemDto>> GetPlansPagedAsync(TimePlanningOverviewFilter filter, CancellationToken ct = default);

    /// <summary>Ejecuciones de un empleado de una planificación; vacío si queda fuera del alcance por departamento.</summary>
    Task<IReadOnlyList<TimePlanningOverviewExecutionDto>> GetPlanEmployeeExecutionsAsync(
        int planEmployeeId, IReadOnlyList<int>? allowedDepartmentIds, CancellationToken ct = default);

    /// <summary>Lista paginada de los empleados de una planificación (con alcance por departamento).</summary>
    Task<PagedResult<TimePlanningOverviewEmployeeDto>> GetPlanEmployeesPagedAsync(
        int planId, int page, int pageSize, int? employeeStatusTypeId, string? search,
        IReadOnlyList<int>? allowedDepartmentIds, CancellationToken ct = default);
}
