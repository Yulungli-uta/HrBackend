using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using WsUtaSystem.Application.DTOs.Parameters;
using WsUtaSystem.Application.DTOs.RefTypes;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;


[ApiController]
[Route("cv/parameters")]
public class ParametersController : ControllerBase
{
    private readonly IParametersService _svc;
    private readonly IMapper _mapper;
    public ParametersController(IParametersService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de Parameters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ParametersDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<ParametersDto>(e));
    }

    /// <summary>Retorna un resultado paginado de parámetros.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
    /// <param name="search">Texto de búsqueda por nombre o descripción.</param>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        Expression<Func<Parameters, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            predicate = p =>
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term));
        }

        var pagedEntities = predicate is not null
            ? await _svc.GetPagedAsync(predicate, page, pageSize, ct)
            : await _svc.GetPagedAsync(page, pageSize, ct);

        var dtoItems = _mapper.Map<List<ParametersDto>>(pagedEntities.Items);

        return Ok(new
        {
            items = dtoItems,
            page = pagedEntities.Page,
            pageSize = pagedEntities.PageSize,
            totalCount = pagedEntities.TotalCount,
            totalPages = pagedEntities.TotalPages,
            hasPreviousPage = pagedEntities.HasPreviousPage,
            hasNextPage = pagedEntities.HasNextPage
        });
    }

    [HttpGet("name/{name}")]
    public async Task<IActionResult> GetByName([FromRoute] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("El nombre no puede estar vacío.");

        var entities = (await _svc.GetByNameAsync(name, ct))?.ToList() ?? new List<Parameters>();

        if (entities.Count == 0)
            return NotFound($"No se encontraron parámetros con el nombre '{name}'");

        // ✅ devuelve DTOs, no entidades
        return Ok(_mapper.Map<List<ParametersDto>>(entities));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    public async Task<IActionResult> Create([FromBody] ParametersCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Parameters>(dto);
        entityObj.CreatedAt = DateTime.Now;
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<ParametersDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ParametersUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Parameters>(dto);
        entityObj.UpdatedAt = DateTime.Now;
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("CATALOGS.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}

