using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.AttendanceCalculations;
using WsUtaSystem.Application.DTOs.StoredProcedures;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("attendance/calculations")]
public class AttendanceCalculationsController : ControllerBase
{
    private readonly IAttendanceCalculationService _attendanceProcessService;
    private readonly IAttendanceCalculationsService _attendanceQueryService;
    private readonly IMapper _mapper;
    private readonly IJobExecutionLogService _jobExecutionLogService;

    public AttendanceCalculationsController(
        IAttendanceCalculationsService attendanceQueryService,
        IMapper mapper,
        IAttendanceCalculationService attendanceProcessService,
        IJobExecutionLogService jobExecutionLogService)
    {
        _attendanceQueryService = attendanceQueryService;
        _mapper = mapper;
        _attendanceProcessService = attendanceProcessService;
        _jobExecutionLogService = jobExecutionLogService;
    }

    /// <summary>Lista todos los registros de AttendanceCalculations.</summary>
    [HttpGet]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var data = await _attendanceQueryService.GetAllAsync(ct);
        return Ok(_mapper.Map<List<AttendanceCalculationsDto>>(data));
    }

    /// <summary>Obtiene un registro por ID.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var entity = await _attendanceQueryService.GetByIdAsync(id, ct);
        return entity is null ? NotFound() : Ok(_mapper.Map<AttendanceCalculationsDto>(entity));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("ATTENDANCE.CREATE")]
    public async Task<IActionResult> Create([FromBody] AttendanceCalculationsCreateDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<AttendanceCalculations>(dto);
        var created = await _attendanceQueryService.CreateAsync(entity, ct);

        var idVal = created?.GetType()
            .GetProperties()
            .FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))
            ?.GetValue(created);

        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<AttendanceCalculationsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("ATTENDANCE.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AttendanceCalculationsUpdateDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<AttendanceCalculations>(dto);
        await _attendanceQueryService.UpdateAsync(id, entity, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("ATTENDANCE.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _attendanceQueryService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Endpoint oficial del nuevo pipeline de asistencia por rango.
    /// </summary>
    [HttpPost("process-range")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> ProcessAttendanceRange(
        [FromBody] AttendanceCalculationRequestDto request,
        CancellationToken ct)
    {
        long? logId = null;
        try
        {
            logId = await _jobExecutionLogService.StartAsync("DailyAttendanceCalculationJob", "Manual", ct);
        }
        catch
        {
            // El logging de auditoría nunca debe bloquear la ejecución real.
        }

        try
        {
            await _attendanceProcessService.ProcessAttendanceRunRangeAsync(
                request.FromDate,
                request.ToDate,
                employeeId: request.EmployeeId,
                ct: ct);
        }
        catch (Exception ex)
        {
            if (logId.HasValue)
            {
                try { await _jobExecutionLogService.FinishAsync(logId.Value, "Failed", ex.Message, ct); }
                catch { /* no bloquear el manejo del error real */ }
            }
            throw;
        }

        if (logId.HasValue)
        {
            try { await _jobExecutionLogService.FinishAsync(logId.Value, "Success", null, ct); }
            catch { /* no bloquear la respuesta */ }
        }

        return Ok(new
        {
            success = true,
            message = "Attendance pipeline executed successfully."
        });
    }

    /// <summary>
    /// Endpoint oficial del nuevo pipeline de asistencia por fecha.
    /// </summary>
    [HttpPost("process-date")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> ProcessAttendanceDate(
        [FromBody] DateTime workDate,
        CancellationToken ct)
    {
        long? logId = null;
        try
        {
            logId = await _jobExecutionLogService.StartAsync("DailyAttendanceCalculationJob", "Manual", ct);
        }
        catch
        {
            // El logging de auditoría nunca debe bloquear la ejecución real.
        }

        try
        {
            await _attendanceProcessService.ProcessAttendanceRunDateAsync(workDate, employeeId: null, ct: ct);
        }
        catch (Exception ex)
        {
            if (logId.HasValue)
            {
                try { await _jobExecutionLogService.FinishAsync(logId.Value, "Failed", ex.Message, ct); }
                catch { /* no bloquear el manejo del error real */ }
            }
            throw;
        }

        if (logId.HasValue)
        {
            try { await _jobExecutionLogService.FinishAsync(logId.Value, "Success", null, ct); }
            catch { /* no bloquear la respuesta */ }
        }

        return Ok(new
        {
            success = true,
            message = "Attendance date pipeline executed successfully."
        });
    }

    /// <summary>
    /// Obsoleto. Usa process-range.
    /// </summary>
    [HttpPost("calculate-range")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> CalculateRange(
        [FromBody] AttendanceCalculationRequestDto request,
        CancellationToken ct)
    {
        await _attendanceProcessService.CalculateRangeAsync(
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            ct);

        return Ok(new
        {
            success = true,
            message = "Legacy attendance calculation completed successfully."
        });
    }

    /// <summary>
    /// Obsoleto. Los minutos nocturnos ahora forman parte del pipeline principal.
    /// </summary>
    [HttpPost("calc-night-minutes")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> CalculateNightMinutes(
        [FromBody] AttendanceCalculationRequestDto request,
        CancellationToken ct)
    {
        await _attendanceProcessService.CalculateNightMinutesAsync(
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            ct);

        return Ok(new
        {
            success = true,
            message = "Legacy night minutes calculation completed successfully."
        });
    }

    /// <summary>
    /// Obsoleto. Las justificaciones ahora forman parte del pipeline principal.
    /// </summary>
    [HttpPost("apply-justifications")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> ProcessApplyJustification(
        [FromBody] AttendanceCalculationRequestDto request,
        CancellationToken ct)
    {
        await _attendanceProcessService.ProcessApplyJustification(
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            ct);

        return Ok(new
        {
            success = true,
            message = "Legacy justifications application completed successfully."
        });
    }

    /// <summary>
    /// Obsoleto. Overtime y recovery ahora forman parte del pipeline principal.
    /// </summary>
    [HttpPost("apply-overtime-recovery")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> ProcessApplyOvertimeRecovery(
        [FromBody] AttendanceCalculationRequestDto request,
        CancellationToken ct)
    {
        await _attendanceProcessService.ProcessApplyOvertimeRecovery(
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            ct);

        return Ok(new
        {
            success = true,
            message = "Legacy overtime recovery application completed successfully."
        });
    }
}