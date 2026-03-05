using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Vacations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("vacations")]
public class VacationsController : ControllerBase
{
    private readonly IVacationsService _svc;
    private readonly IMapper _mapper;

    public VacationsController(IVacationsService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<VacationsDto>>(await _svc.GetAllAsync(ct)));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<VacationsDto>(e));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByEmployeeId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<VacationsDto>>(e));
    }

    [HttpGet("bossId/{employeeId:int}")]
    public async Task<IActionResult> GetByImmediateBossId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByImmediateBossId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<VacationsDto>>(e));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VacationsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Vacations>(dto);
        var created = await _svc.CreateWithBalanceCheckAsync(entityObj, ct);

        var idVal = created?.GetType()?.GetProperties()
            ?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))
            ?.GetValue(created);

        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<VacationsDto>(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] VacationsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Vacations>(dto);

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
