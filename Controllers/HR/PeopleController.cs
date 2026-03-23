using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.People;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Produces("application/json")]
[Route("people")]
public class PeopleController : ControllerBase
{
    private readonly IPeopleService _svc;
    private readonly IMapper _mapper;

    public PeopleController(IPeopleService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    /// <summary>Lista todos los registros de People.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var entities = await _svc.GetAllAsync(ct);
        var dtoList = _mapper.Map<List<PeopleDto>>(entities);
        return Ok(dtoList);
    }

    /// <summary>Retorna un resultado paginado de registros de People.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
    /// <param name="search">Texto de búsqueda por nombre, apellido, cédula o email.</param>
    /// <param name="sortBy">Campo de ordenamiento (opcional).</param>
    /// <param name="sortDirection">Dirección del orden: asc | desc (opcional).</param>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        System.Linq.Expressions.Expression<Func<People, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            predicate = p =>
                (p.FirstName != null && p.FirstName.ToLower().Contains(term)) ||
                (p.LastName != null && p.LastName.ToLower().Contains(term)) ||
                (p.IdCard != null && p.IdCard.ToLower().Contains(term)) ||
                (p.Email != null && p.Email.ToLower().Contains(term));
        }

        var pagedEntities = predicate is not null
            ? await _svc.GetPagedAsync(predicate, page, pageSize, ct)
            : await _svc.GetPagedAsync(page, pageSize, ct);

        var dtoItems = _mapper.Map<List<PeopleDto>>(pagedEntities.Items);

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

    /// <summary>Obtiene un registro por ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var entity = await _svc.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        var dto = _mapper.Map<PeopleDto>(entity);
        return Ok(dto);
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PeopleCreateDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<People>(dto);
        var created = await _svc.CreateAsync(entity, ct);

        var idVal = created?.GetType()?.GetProperties()
            ?.FirstOrDefault(p =>
                p.Name.Equals("Id") ||
                p.Name.EndsWith("Id") ||
                p.Name.EndsWith("ID"))
            ?.GetValue(created);

        return CreatedAtAction(
            nameof(GetById),
            new { id = idVal },
            _mapper.Map<PeopleDto>(created)
        );
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] PeopleUpdateDto dto,
        CancellationToken ct)
    {
        var entity = _mapper.Map<People>(dto);
        await _svc.UpdateAsync(id, entity, ct);
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