using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.MassVacationPlan;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Planificación masiva de vacaciones (cierre colectivo institucional o por
/// departamento). No genera filas individuales en HR.tbl_Vacations — el saldo se
/// descuenta en lote al ejecutar, y el cruce con asistencia/historial personal lee
/// directamente este plan + su tabla de exclusiones.
/// </summary>
[ApiController]
[Route("mass-vacation-plans")]
public class MassVacationPlansController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IMassVacationPlanService _svc;
    private readonly ICurrentUserService _currentUser;

    public MassVacationPlansController(IMassVacationPlanService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpGet("paged")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default) =>
        Ok(await _svc.GetPagedAsync(page, pageSize, search, fromDate, toDate, ct));

    [HttpGet("{id:int}")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var dto = await _svc.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:int}/roster")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetRoster([FromRoute] int id, CancellationToken ct) =>
        Ok(await _svc.GetRosterAsync(id, ct));

    /// <summary>Planes institucionales ejecutados que le aplican a este empleado — para mostrar en su historial personal de vacaciones.</summary>
    [HttpGet("employee/{employeeId:int}/applicable")]
    [RequirePermission("VACATIONS.READ")]
    public async Task<IActionResult> GetApplicableForEmployee([FromRoute] int employeeId, CancellationToken ct)
    {
        if (_currentUser.EmployeeId != employeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede consultar los planes institucionales de otro empleado.");

        return Ok(await _svc.GetApplicablePlansForEmployeeAsync(employeeId, ct));
    }

    [HttpPost]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> Create([FromBody] MassVacationPlanCreateDto dto, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para crear planes de vacaciones masivas.");

        try
        {
            var created = await _svc.CreateAsync(dto, _currentUser.EmployeeId, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.PlanId }, created);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Edita un plan mientras está en Planificado (mismas validaciones que crear).</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] MassVacationPlanUpdateDto dto, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para editar planes de vacaciones masivas.");

        try
        {
            var updated = await _svc.UpdateAsync(id, dto, _currentUser.EmployeeId, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/exclusion")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> SetExclusion([FromRoute] int id, [FromBody] MassVacationPlanExclusionSetDto dto, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para modificar exclusiones de un plan de vacaciones masivas.");

        try
        {
            await _svc.SetExclusionAsync(id, dto, _currentUser.EmployeeId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Anula un plan mientras está en Planificado. Una vez En Ejecución ya se
    /// descontó saldo automáticamente por fecha; no hay endpoint de ejecución manual.</summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> Cancel([FromRoute] int id, [FromBody] MassVacationPlanCancelDto? dto, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para anular un plan de vacaciones masivas.");

        try
        {
            await _svc.CancelAsync(id, dto?.Reason, _currentUser.EmployeeId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Soporte/pruebas: fuerza la misma corrida que hace el job diario
    /// (DailyMassVacationPlanTransitionJob) sin esperar a la ejecución programada.</summary>
    [HttpPost("process-due-transitions")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> ProcessDueTransitions(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para forzar la transición de planes de vacaciones masivas.");

        return Ok(await _svc.ProcessDueTransitionsAsync(_currentUser.EmployeeId, ct));
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new { message });
}
