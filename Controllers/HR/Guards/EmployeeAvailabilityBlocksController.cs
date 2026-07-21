using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("employee-availability-blocks")]
public class EmployeeAvailabilityBlocksController : ControllerBase
{
    private readonly IEmployeeAvailabilityService _svc;
    public EmployeeAvailabilityBlocksController(IEmployeeAvailabilityService svc) => _svc = svc;

    [HttpGet]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetBlocks([FromQuery] EmployeeAvailabilityFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetBlocksAsync(filter, ct));

    [HttpGet("paged")]
    [RequirePermission("GUARDS.READ")]
    public async Task<IActionResult> GetBlocksPaged(
        [FromQuery] EmployeeAvailabilityFilterDto filter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetBlocksPagedAsync(filter, page, pageSize, ct));
    }

    [HttpPost("manual")]
    [RequirePermission("GUARDS.CREATE")]
    public async Task<IActionResult> CreateManual([FromBody] CreateManualAvailabilityBlockDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateManualBlockAsync(dto, ct));

    [HttpPost("sync-permissions")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> SyncPermissions([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, CancellationToken ct) =>
        Ok(await _svc.SyncPermissionsAsync(startDate, endDate, ct));

    [HttpPost("sync-vacations")]
    [RequirePermission("GUARDS.UPDATE")]
    public async Task<IActionResult> SyncVacations([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, CancellationToken ct) =>
        Ok(await _svc.SyncVacationsAsync(startDate, endDate, ct));
}
