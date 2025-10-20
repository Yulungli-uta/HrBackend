using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.StoredProcedures;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/payroll")]
public class PayrollSubsidiesController : ControllerBase
{
    private readonly IPayrollSubsidiesService _svc;

    public PayrollSubsidiesController(IPayrollSubsidiesService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Calcula los subsidios y recargos (nocturnos/feriados) para un período de nómina.
    /// </summary>
    /// <param name="request">Período en formato YYYY-MM</param>
    [HttpPost("subsidies")]
    public async Task<IActionResult> CalculateSubsidies([FromBody] PayrollPeriodRequestDto request, CancellationToken ct)
    {
        await _svc.CalculateSubsidiesAsync(request.Period, ct);
        return Ok(new { success = true, message = "Payroll subsidies calculation completed successfully." });
    }
}

