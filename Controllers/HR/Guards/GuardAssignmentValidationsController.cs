using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Guards;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-assignment-validations")]
public class GuardAssignmentValidationsController : ControllerBase
{
    private readonly IGuardAssignmentValidationService _svc;
    public GuardAssignmentValidationsController(IGuardAssignmentValidationService svc) => _svc = svc;

    [HttpGet("by-planning/{planningId:int}")]
    public async Task<IActionResult> GetByPlanning(int planningId, CancellationToken ct) =>
        Ok(await _svc.GetByPlanningAsync(planningId, ct));

    [HttpGet("by-planning/{planningId:int}/paged")]
    public async Task<IActionResult> GetByPlanningPaged(
        int planningId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetByPlanningPagedAsync(planningId, page, pageSize, ct));
    }

    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId, [FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await _svc.GetByEmployeeAsync(employeeId, limit, ct));
}
