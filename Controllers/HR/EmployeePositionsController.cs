using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>Posiciones vigentes de los empleados: principal, segundo régimen (doble régimen), autoridad y temporales.</summary>
[ApiController]
[Route("employee-positions")]
public class EmployeePositionsController : ControllerBase
{
    private readonly IVwEmployeePositionService _svc;

    public EmployeePositionsController(IVwEmployeePositionService svc) => _svc = svc;

    /// <summary>Posiciones vigentes de un empleado.</summary>
    /// <param name="employeeId">ID del empleado.</param>
    [HttpGet("by-employee/{employeeId:int}")]
    [RequirePermission("EMPLOYEES.READ")]
    public async Task<IActionResult> GetByEmployee([FromRoute] int employeeId, CancellationToken ct) =>
        Ok(await _svc.GetByEmployeeAsync(employeeId, ct));

    /// <summary>Posiciones vigentes de varios empleados (los de la página visible; máximo 200).</summary>
    /// <param name="employeeIds">IDs separados por coma, p. ej. 4,5,9.</param>
    [HttpGet]
    [RequirePermission("EMPLOYEES.READ")]
    public async Task<IActionResult> GetByEmployees([FromQuery] string employeeIds, CancellationToken ct)
    {
        var ids = (employeeIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var id) ? id : (int?)null).ToList();
        if (ids.Any(i => i is null)) return BadRequest("employeeIds debe ser una lista de números separados por coma.");
        return Ok(await _svc.GetByEmployeesAsync(ids.Select(i => i!.Value).ToList(), ct));
    }
}
