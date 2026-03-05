using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.StoredProcedures;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("cv/recovery")]
public class RecoveryController : ControllerBase
{
    private readonly IRecoveryService _svc;

    public RecoveryController(IRecoveryService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Consolida recuperaciones de tiempo para restar deuda de minutos adeudados.
    /// </summary>
    /// <param name="request">Rango de fechas y empleado opcional</param>
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyRecovery([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc.ApplyRecoveryAsync(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Recovery applied successfully." });
    }
}

