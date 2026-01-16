using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Permissions;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionsService _svc;
    private readonly IMapper _mapper;

    public PermissionsController(IPermissionsService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<PermissionsDto>>(await _svc.GetAllAsync(ct)));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<PermissionsDto>(e));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmplopyeeId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByEmployeeId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    [HttpGet("bossId/{employeeId:int}")]
    public async Task<IActionResult> GetByImmediateBossId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByImmediateBossId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PermissionsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Permissions>(dto);
        var created = await _svc.CreateWithBalanceCheckAsync(entityObj, ct);

        var idVal = created?.GetType()?.GetProperties()
            ?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))
            ?.GetValue(created);

        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PermissionsDto>(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PermissionsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Permissions>(dto);

        // ✅ Antes: UpdateAsync (no afectaba saldo)
        // ✅ Ahora: UpdateBalanceAffectAsync (sí afecta saldo)
        await _svc.UpdateBalanceAffectAsync(id, entityObj, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
