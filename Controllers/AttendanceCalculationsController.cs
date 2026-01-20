using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.AttendanceCalculations;
using WsUtaSystem.Application.DTOs.StoredProcedures;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("attendance/calculations")]
public class AttendanceCalculationsController : ControllerBase
{
    private readonly IAttendanceCalculationService _svc1;
    private readonly IAttendanceCalculationsService _svc;
    private readonly IMapper _mapper;
    public AttendanceCalculationsController(IAttendanceCalculationsService svc, IMapper mapper, IAttendanceCalculationService svc1)
    {
        _svc = svc;
        _mapper = mapper;
        _svc1 = svc1;
    }

    /// <summary>Lista todos los registros de AttendanceCalculations.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<AttendanceCalculationsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<AttendanceCalculationsDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AttendanceCalculationsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<AttendanceCalculations>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<AttendanceCalculationsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AttendanceCalculationsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<AttendanceCalculations>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("calculate-range")]
    public async Task<IActionResult> CalculateRange([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc1.CalculateRangeAsync(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Attendance calculation completed successfully." });
    }

    /// <summary>
    /// Calcula los minutos nocturnos trabajados para un rango de fechas.
    /// </summary>
    /// <param name="request">Rango de fechas y empleado opcional</param>
    [HttpPost("calc-night-minutes")]
    public async Task<IActionResult> CalculateNightMinutes([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc1.CalculateNightMinutesAsync(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Night minutes calculation completed successfully." });
    }

    /// <summary>
    /// Procesa el rango completo de asistencia (orquestador principal).
    /// </summary>
    /// <param name="request">Rango de fechas</param>
    [HttpPost("process-range")]
    public async Task<IActionResult> ProcessAttendanceRange([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc1.ProcessAttendanceRange(request.FromDate, request.ToDate, ct);
        return Ok(new { success = true, message = "Attendance range processing completed successfully." });
    }

    /// <summary>
    /// Aplica justificaciones aprobadas para anular atrasos o ausencias.
    /// </summary>
    /// <param name="request">Rango de fechas y empleado opcional</param>
    [HttpPost("apply-justifications")]
    public async Task<IActionResult> ProcessApplyJustification([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc1.ProcessApplyJustification(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Justifications applied successfully." });
    }

    /// <summary>
    /// Procesa el cálculo y aplicación de recuperación de horas extra.
    /// </summary>
    /// <param name="request">Rango de fechas y empleado opcional</param>
    [HttpPost("apply-overtime-recovery")]
    public async Task<IActionResult> ProcessApplyOvertimeRecovery([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc1.ProcessApplyOvertimeRecovery(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Overtime recovery applied successfully." });
    }
}
