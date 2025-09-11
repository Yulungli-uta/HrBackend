using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Serilog.Core;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.People;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Produces("application/json")]
[Route("people")]
public class PeopleController : ControllerBase
{
    private readonly IPeopleService _svc;
    private readonly IMapper _mapper;
   
    public PeopleController(IPeopleService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de People.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<PeopleDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<PeopleDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PeopleCreateDto dto, CancellationToken ct)
    {
        Console.WriteLine($"**********Info: CREATE " + dto.CountryId +
            " provincia: " + dto.ProvinceId + " canton: " + dto.CantonId);
        Console.WriteLine("***********json completo: " + System.Text.Json.JsonSerializer.Serialize(dto));
        var entityObj = _mapper.Map<People>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PeopleDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PeopleUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<People>(dto);
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
