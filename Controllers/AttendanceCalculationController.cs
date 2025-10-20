using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.StoredProcedures;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/attendance")]
public class AttendanceCalculationController : ControllerBase
{
    private readonly IAttendanceCalculationService _svc;

    public AttendanceCalculationController(IAttendanceCalculationService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Ejecuta el cálculo masivo de asistencia para un rango de fechas.
    /// </summary>
    /// <param name="request">Rango de fechas y empleado opcional</param>
    [HttpPost("calculate-range")]
    public async Task<IActionResult> CalculateRange([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc.CalculateRangeAsync(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Attendance calculation completed successfully." });
    }

    /// <summary>
    /// Calcula los minutos nocturnos trabajados para un rango de fechas.
    /// </summary>
    /// <param name="request">Rango de fechas y empleado opcional</param>
    [HttpPost("calc-night-minutes")]
    public async Task<IActionResult> CalculateNightMinutes([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc.CalculateNightMinutesAsync(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Night minutes calculation completed successfully." });
    }
}

