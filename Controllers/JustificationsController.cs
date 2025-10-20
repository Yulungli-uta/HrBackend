using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.StoredProcedures;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/justifications")]
public class JustificationsController : ControllerBase
{
    private readonly IJustificationsService _svc;

    public JustificationsController(IJustificationsService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Aplica justificaciones aprobadas para anular atrasos o ausencias.
    /// </summary>
    /// <param name="request">Rango de fechas y empleado opcional</param>
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyJustifications([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc.ApplyJustificationsAsync(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Justifications applied successfully." });
    }
}

