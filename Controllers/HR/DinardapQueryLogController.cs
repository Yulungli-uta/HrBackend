using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Dinardap;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>Consulta de la auditoría de consultas a DINARDAP (quién consultó, qué paquete, cuándo y con qué resultado).</summary>
[ApiController]
[Route("dinardap-query-log")]
public sealed class DinardapQueryLogController : ControllerBase
{
    private readonly IDinardapQueryAuditService _audit;

    public DinardapQueryLogController(IDinardapQueryAuditService audit)
    {
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    /// <summary>
    /// Lista paginada (más reciente primero). La identificación consultada se muestra siempre enmascarada.
    /// Filtros opcionales: rango de fechas, usuario (correo o nombre), paquete (REGISTRO_CIVIL | TITULOS | TCE),
    /// origen (USER | SYSTEM) y resultado (OK | UNAVAILABLE).
    /// </summary>
    [HttpGet]
    [RequirePermission("AUDIT.READ")]
    public async Task<IActionResult> Search(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? user,
        [FromQuery] string? package,
        [FromQuery] string? origin,
        [FromQuery] string? outcome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return BadRequest(new { message = "La fecha \"desde\" no puede ser posterior a la fecha \"hasta\"." });

        var result = await _audit.SearchAsync(new DinardapQueryLogFilter(from, to, user, package, origin, outcome), page, pageSize, ct);
        return Ok(result);
    }
}
