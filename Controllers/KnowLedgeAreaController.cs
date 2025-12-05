using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Quartz.Util;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.KnowledgeArea;

using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/knowledgeArea")]
public class KnowledgeAreaController : ControllerBase
{
    private readonly IKnowledgeAreaService _svc;
    private readonly IMapper _mapper;
    public KnowledgeAreaController(IKnowledgeAreaService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<KnowledgeAreaDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<KnowledgeAreaDto>(e));
    }

    [HttpGet("parentId/{parentId}")]
    public async Task<IActionResult> GetByParentId([FromRoute] int parentId, CancellationToken ct)
    { 
        Console.WriteLine("*************************ParentId: " + parentId);
        // Validación básica
        //if (parentId<=0)
        //{
        //    return BadRequest("La parentId no puede estar vacía");
        //}

        var entities = await _svc.GetByParentAsync(parentId, ct);

        // Si no se encuentran resultados, devolver un 404
        if (!entities.Any())
        {
            return NotFound($"No se encontraron registros para la categoría '{parentId}'");
        }

        return Ok(_mapper.Map<List<KnowledgeAreaDto>>(entities));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KnowledgeAreaCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<KnowledgeArea>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<KnowledgeAreaDto>(created));
    }
      
    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] KnowledgeAreaUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<KnowledgeArea>(dto);
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
