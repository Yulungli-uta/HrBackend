using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-shift-planning")]
public class GuardShiftPlanningController : ControllerBase
{
    private readonly IGuardShiftPlanningService _svc;
    public GuardShiftPlanningController(IGuardShiftPlanningService svc) => _svc = svc;

    /// <summary>Retorna el panel resumen de guardias.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct) =>
        Ok(await _svc.GetDashboardAsync(ct));

    /// <summary>Retorna planificaciones en formato calendario.</summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] GuardShiftCalendarFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetCalendarAsync(filter, ct));

    /// <summary>Retorna planificación por ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Crea una planificación manual.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuardShiftPlanningDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.PlanningId }, created);
    }

    /// <summary>Genera planificación automática para un grupo y rango de fechas (flujo legado).</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateGuardShiftPlanningRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.GenerateAsync(dto, ct));

    /// <summary>Previsualiza la generación sin guardar. Retorna los turnos calculados y sus conflictos.</summary>
    [HttpPost("generate-preview")]
    public async Task<IActionResult> GeneratePreview([FromBody] GeneratePreviewRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.GeneratePreviewAsync(dto, ct));

    /// <summary>Confirma y guarda solo los registros válidos. Los conflictos quedan registrados.</summary>
    [HttpPost("generate-confirm")]
    public async Task<IActionResult> GenerateConfirm([FromBody] GeneratePreviewRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.GenerateConfirmAsync(dto, ct));

    /// <summary>Retorna el cronograma estructurado por ubicación/turno/fecha.</summary>
    [HttpGet("schedule-board")]
    public async Task<IActionResult> GetScheduleBoard([FromQuery] ScheduleBoardFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetScheduleBoardAsync(filter, ct));

    /// <summary>Retorna el detalle completo de una planificación para el panel lateral.</summary>
    [HttpGet("{id:int}/detail")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var result = await _svc.GetPlanningDetailAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Valida una asignación antes de crear la planificación.</summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateGuardAssignmentRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.ValidateAssignmentAsync(dto, ct));
}
