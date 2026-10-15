using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TimePlanning;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Consulta de solo lectura de las planificaciones de horas extra (y recuperación) con los
/// empleados incluidos y sus estados, para Recursos Humanos.
/// </summary>
[ApiController]
[Route("planning/overview")]
public sealed class TimePlanningOverviewController : ControllerBase
{
    private readonly ITimePlanningOverviewService _service;
    private readonly ICurrentUserService _currentUser;

    public TimePlanningOverviewController(ITimePlanningOverviewService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>Lista paginada de planificaciones con totales de empleados y horas.</summary>
    [HttpGet]
    [RequirePermission("TIME_PLANNING.READ")]
    [ProducesResponseType(typeof(PagedResult<TimePlanningOverviewItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans([FromQuery] TimePlanningOverviewFilter filter, CancellationToken ct)
        => Ok(await _service.GetPlansAsync(filter, _currentUser.EmployeeId, ct));

    /// <summary>Ejecuciones (horas trabajadas) de un empleado dentro de una planificación.</summary>
    [HttpGet("employees/{planEmployeeId:int}/executions")]
    [RequirePermission("TIME_PLANNING.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<TimePlanningOverviewExecutionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlanEmployeeExecutions([FromRoute] int planEmployeeId, CancellationToken ct)
        => Ok(await _service.GetPlanEmployeeExecutionsAsync(planEmployeeId, _currentUser.EmployeeId, ct));

    /// <summary>Empleados de una planificación (paginado) con su estado y horas asignadas/ejecutadas.</summary>
    [HttpGet("{planId:int}/employees")]
    [RequirePermission("TIME_PLANNING.READ")]
    [ProducesResponseType(typeof(PagedResult<TimePlanningOverviewEmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlanEmployees(
        [FromRoute] int planId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? employeeStatusTypeId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await _service.GetPlanEmployeesAsync(planId, page, pageSize, employeeStatusTypeId, search, _currentUser.EmployeeId, ct));
}
