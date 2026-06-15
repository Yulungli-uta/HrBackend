using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;

namespace WsUtaSystem.Controllers.HR.Guards;

[ApiController]
[Route("guard-vacation-requests")]
public class GuardVacationRequestsController : ControllerBase
{
    private readonly IGuardVacationService _svc;
    public GuardVacationRequestsController(IGuardVacationService svc) => _svc = svc;

    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId, CancellationToken ct) =>
        Ok(await _svc.GetRequestsByEmployeeAsync(employeeId, ct));

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _svc.GetRequestsPagedAsync(page, pageSize, status, employeeId, startDate, endDate, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _svc.GetRequestByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("change-dates")]
    public async Task<IActionResult> CreateChangeDates([FromBody] CreateChangeDatesRequestDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateChangeDatesRequestAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.GuardVacationRequestId }, created);
    }

    [HttpPost("accumulate")]
    public async Task<IActionResult> CreateAccumulate([FromBody] CreateAccumulateRequestDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateAccumulateRequestAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.GuardVacationRequestId }, created);
    }

    [HttpPost("{id:int}/submit-to-direction")]
    public async Task<IActionResult> SubmitToDirection(int id, [FromBody] SubmitToDirectionDto dto, CancellationToken ct) =>
        Ok(await _svc.SubmitRequestToDirectionAsync(id, dto, ct));

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveGuardVacationRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.ApproveRequestAsync(id, dto, ct));

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectGuardVacationRequestDto dto, CancellationToken ct) =>
        Ok(await _svc.RejectRequestAsync(id, dto, ct));
}
