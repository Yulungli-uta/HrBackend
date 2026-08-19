using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using WsUtaSystem.Application.DTOs.PersonnelActionType;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("personnel-action-type")]
public sealed class PersonnelActionTypeController : ControllerBase
{
    private readonly IPersonnelActionTypeService _svc;
    private readonly IMapper _mapper;

    public PersonnelActionTypeController(IPersonnelActionTypeService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    /// <summary>Lista todos los tipos de acción de personal.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PersonnelActionTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var entities = await _svc.GetAllAsync(ct);
        return Ok(_mapper.Map<List<PersonnelActionTypeDto>>(entities));
    }

    /// <summary>Lista solo los tipos de acción activos.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<PersonnelActionTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var entities = await _svc.GetAllActiveAsync(ct);
        return Ok(_mapper.Map<List<PersonnelActionTypeDto>>(entities));
    }

    /// <summary>Obtiene un tipo de acción por ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PersonnelActionTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var entity = await _svc.GetByIdAsync(id, ct);
        return entity is null ? NotFound() : Ok(_mapper.Map<PersonnelActionTypeDto>(entity));
    }

    /// <summary>Retorna un resultado paginado de tipos de acción de personal.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
    /// <param name="search">Texto de búsqueda por nombre, código o descripción.</param>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        Expression<Func<PersonnelActionType, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            predicate = a =>
                a.Name.ToLower().Contains(term) ||
                a.Code.ToLower().Contains(term) ||
                (a.Description != null && a.Description.ToLower().Contains(term));
        }

        var pagedEntities = predicate is not null
            ? await _svc.GetPagedAsync(predicate, page, pageSize, ct)
            : await _svc.GetPagedAsync(page, pageSize, ct);

        var dtoItems = _mapper.Map<List<PersonnelActionTypeDto>>(pagedEntities.Items);

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

    /// <summary>
    /// Genera y reserva el siguiente número de documento para el tipo indicado.
    /// El número tiene el formato {prefix}-{year}-{seq:D3} (ej: DAP-2026-001).
    /// </summary>
    [HttpPost("{id:int}/next-number")]
    [ProducesResponseType(typeof(NextDocumentNumberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NextNumber([FromRoute] int id, CancellationToken ct)
    {
        var result = await _svc.GetNextNumberAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Crea un nuevo tipo de acción de personal.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    [ProducesResponseType(typeof(PersonnelActionTypeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] PersonnelActionTypeCreateDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<PersonnelActionType>(dto);
        entity.NumberingYear = DateTime.Now.Year;
        entity.NumberingLastSequence = 0;
        var created = await _svc.CreateAsync(entity, ct);
        return CreatedAtAction(nameof(GetById),
            new { id = created.PersonnelActionTypeId },
            _mapper.Map<PersonnelActionTypeDto>(created));
    }

    /// <summary>Actualiza un tipo de acción existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id, [FromBody] PersonnelActionTypeUpdateDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<PersonnelActionType>(dto);
        await _svc.UpdateAsync(id, entity, ct);
        return NoContent();
    }

    /// <summary>Elimina un tipo de acción por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("CATALOGS.DELETE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Actualiza únicamente la plantilla predeterminada, sin afectar el resto del tipo de acción.</summary>
    [HttpPatch("{id:int}/default-template")]
    [RequirePermission("CATALOGS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultTemplate(
        [FromRoute] int id, [FromBody] SetDefaultTemplateRequest body, CancellationToken ct)
    {
        await _svc.SetDefaultTemplateAsync(id, body.TemplateId, ct);
        return NoContent();
    }
}
