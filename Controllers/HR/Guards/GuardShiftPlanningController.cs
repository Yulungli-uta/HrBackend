using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-shift-planning")]
public class GuardShiftPlanningController : ControllerBase
{
    private readonly IGuardShiftPlanningService _svc;
    public GuardShiftPlanningController(IGuardShiftPlanningService svc) => _svc = svc;

    /// <summary>Retorna el panel resumen de guardias.</summary>
    [HttpGet("dashboard")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct) =>
        Ok(await _svc.GetDashboardAsync(ct));

    /// <summary>Retorna planificaciones en formato calendario.</summary>
    [HttpGet("calendar")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetCalendar([FromQuery] GuardShiftCalendarFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetCalendarAsync(filter, ct));

    /// <summary>Retorna planificación por ID.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Crea una planificación manual.</summary>
    [HttpPost]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateGuardShiftPlanningDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.PlanningId }, created);
    }

    /// <summary>Crea la misma asignación manual repetida semanalmente durante N semanas.</summary>
    [HttpPost("create-recurring")]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> CreateRecurring([FromBody] CreateRecurringGuardShiftPlanningDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _svc.CreateRecurringAsync(dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Genera planificación automática para un grupo y rango de fechas (flujo legado).</summary>
    [HttpPost("generate")]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> Generate([FromBody] GenerateGuardShiftPlanningRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.GenerateAsync(dto, ct));

    /// <summary>Previsualiza la generación sin guardar. Retorna los turnos calculados y sus conflictos.</summary>
    [HttpPost("generate-preview")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GeneratePreview([FromBody] GeneratePreviewRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.GeneratePreviewAsync(dto, ct));

    /// <summary>Confirma y guarda solo los registros válidos. Los conflictos quedan registrados.</summary>
    [HttpPost("generate-confirm")]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> GenerateConfirm([FromBody] GeneratePreviewRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.GenerateConfirmAsync(dto, ct));

    /// <summary>Retorna el cronograma estructurado por ubicación/turno/fecha.</summary>
    [HttpGet("schedule-board")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetScheduleBoard([FromQuery] ScheduleBoardFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetScheduleBoardAsync(filter, ct));

    /// <summary>Retorna el detalle completo de una planificación para el panel lateral.</summary>
    [HttpGet("{id:int}/detail")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var result = await _svc.GetPlanningDetailAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Valida una asignación antes de crear la planificación.</summary>
    [HttpPost("validate")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> Validate([FromBody] ValidateGuardAssignmentRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.ValidateAssignmentAsync(dto, ct));

    /// <summary>
    /// Verifica si el módulo de guardias tiene todos los pre-requisitos configurados
    /// para generar planificación en la fecha indicada.
    /// </summary>
    [HttpGet("readiness-check")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> ReadinessCheck([FromQuery] DateOnly targetDate, CancellationToken ct) =>
        Ok(await _svc.GetReadinessCheckAsync(targetDate, ct));

    /// <summary>Cancela (no borra) una planificación individual.</summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> CancelPlanning(int id, [FromBody] CancelGuardShiftPlanningDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _svc.CancelPlanningAsync(id, dto, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Cancela en bloque las planificaciones activas de un grupo en un rango de fechas.</summary>
    [HttpPost("cancel-range")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> CancelPlanningRange([FromBody] CancelGuardShiftPlanningRangeDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _svc.CancelPlanningRangeAsync(dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
