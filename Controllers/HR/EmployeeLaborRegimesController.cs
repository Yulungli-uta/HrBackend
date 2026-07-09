using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.EmployeeLaborRegime;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("employee-labor-regimes")]
public class EmployeeLaborRegimesController : ControllerBase
{
    private readonly IEmployeeLaborRegimeService _svc;
    private readonly ICurrentUserService _currentUser;

    public EmployeeLaborRegimesController(IEmployeeLaborRegimeService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId, CancellationToken ct)
        => Ok(await _svc.GetByEmployeeAsync(employeeId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeLaborRegimeCreateDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(dto, _currentUser.EmployeeId, ct);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id, [FromBody] EmployeeLaborRegimeCloseDto dto, CancellationToken ct)
    {
        var closed = await _svc.CloseAsync(id, dto, _currentUser.EmployeeId, ct);
        return closed is null ? NotFound(new { message = "No existe." }) : Ok(closed);
    }
}
