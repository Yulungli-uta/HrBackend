using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.UserAccessScope;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("user-access-scopes")]
public class UserAccessScopesController : ControllerBase
{
    private readonly IUserAccessScopeService _svc;
    private readonly ICurrentUserService _currentUser;

    public UserAccessScopesController(IUserAccessScopeService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _svc.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserAccessScopeCreateDto dto, CancellationToken ct)
    {
        try
        {
            var changedBy = _currentUser.Email ?? _currentUser.UserName ?? "system";
            var created = await _svc.CreateAsync(dto, changedBy, ct);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserAccessScopeUpdateDto dto, CancellationToken ct)
    {
        var changedBy = _currentUser.Email ?? _currentUser.UserName ?? "system";
        var updated = await _svc.UpdateAsync(id, dto, changedBy, ct);
        return updated is null ? NotFound(new { message = "No existe." }) : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var changedBy = _currentUser.Email ?? _currentUser.UserName ?? "system";
        var deleted = await _svc.DeleteAsync(id, changedBy, ct);
        return deleted ? Ok(new { message = "Eliminado" }) : NotFound(new { message = "No existe." });
    }

    [HttpGet("history/{employeeId:int}")]
    public async Task<IActionResult> GetHistory(int employeeId, CancellationToken ct)
        => Ok(await _svc.GetHistoryAsync(employeeId, ct));
}
