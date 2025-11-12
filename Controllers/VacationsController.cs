using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.Vacations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("vacations")]
public class VacationsController : ControllerBase
{
    private readonly IVacationsService _svc;
    private readonly IMapper _mapper;
    public VacationsController(IVacationsService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de Vacations.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<VacationsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<VacationsDto>(e));
    }

    /// <summary>Obtiene un registro por EmployeeId.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByEmployeeId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<VacationsDto>>(e));
        //return e is null ? NotFound() : Ok(_mapper.Map<VacationsDto>(e));
    }

    [HttpGet("bossId/{employeeId:int}")]
    public async Task<IActionResult> GetByImmediateBossId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByImmediateBossId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<VacationsDto>>(e));
        //return e is null ? NotFound() : Ok(_mapper.Map<VacationsDto>(e));
    }


    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VacationsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Vacations>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<VacationsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] VacationsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Vacations>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
