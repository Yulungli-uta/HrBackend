using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.StoredProcedures;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/overtime")]
public class OvertimePriceController : ControllerBase
{
    private readonly IOvertimePriceService _svc;

    public OvertimePriceController(IOvertimePriceService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Calcula el precio de las horas extra para un período específico.
    /// </summary>
    /// <param name="request">Período en formato YYYY-MM</param>
    [HttpPost("price")]
    public async Task<IActionResult> CalculateOvertimePrice([FromBody] PayrollPeriodRequestDto request, CancellationToken ct)
    {
        await _svc.CalculateOvertimePriceAsync(request.Period, ct);
        return Ok(new { success = true, message = "Overtime price calculation completed successfully." });
    }
}

