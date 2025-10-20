using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.StoredProcedures;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/payroll")]
public class PayrollDiscountsController : ControllerBase
{
    private readonly IPayrollDiscountsService _svc;

    public PayrollDiscountsController(IPayrollDiscountsService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Calcula los descuentos por atrasos y ausencias para un período de nómina.
    /// </summary>
    /// <param name="request">Período en formato YYYY-MM</param>
    [HttpPost("discounts")]
    public async Task<IActionResult> CalculateDiscounts([FromBody] PayrollPeriodRequestDto request, CancellationToken ct)
    {
        await _svc.CalculateDiscountsAsync(request.Period, ct);
        return Ok(new { success = true, message = "Payroll discounts calculation completed successfully." });
    }
}

