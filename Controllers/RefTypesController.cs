using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.RefTypes;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("ref/types")]
public class RefTypesController : ControllerBase
{
    private readonly IRefTypesService _svc;
    private readonly IMapper _mapper;
    public RefTypesController(IRefTypesService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de RefTypes.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<RefTypesDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<RefTypesDto>(e));
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory([FromRoute] string category, CancellationToken ct)
    {
        // Validación básica
        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest("La categoría no puede estar vacía");
        }

        var entities = await _svc.GetByCategoryAsync(category, ct);

        // Si no se encuentran resultados, devolver un 404
        if (!entities.Any())
        {
            return NotFound($"No se encontraron registros para la categoría '{category}'");
        }

        return Ok(_mapper.Map<List<RefTypesDto>>(entities));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RefTypesCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<RefTypes>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<RefTypesDto>(created));
    }
      
    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] RefTypesUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<RefTypes>(dto);
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
