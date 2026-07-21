using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.PersonnelMovements;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("personnel-movements")]
public class PersonnelMovementsController : ControllerBase
{
    private readonly IPersonnelMovementsService _svc;
    private readonly IMapper _mapper;
    public PersonnelMovementsController(IPersonnelMovementsService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de PersonnelMovements.</summary>
    [HttpGet]
    [RequirePermission("PERSONNEL_ACTIONS.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<PersonnelMovementsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("PERSONNEL_ACTIONS.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<PersonnelMovementsDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("PERSONNEL_ACTIONS.CREATE")]
    public async Task<IActionResult> Create([FromBody] PersonnelMovementsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<PersonnelMovements>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PersonnelMovementsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("PERSONNEL_ACTIONS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PersonnelMovementsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<PersonnelMovements>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("PERSONNEL_ACTIONS.CANCEL")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
