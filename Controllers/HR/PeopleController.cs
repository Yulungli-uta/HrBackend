using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Common;
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
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<PeopleDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Retorna un resultado paginado de registros de People.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
    /// <param name="sortBy">Campo de ordenamiento (opcional).</param>
    /// <param name="sortDirection">Dirección del orden: asc | desc (opcional).</param>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var pagedEntities = await _svc.GetPagedAsync(page, pageSize, ct);
        var pagedDto = new PagedResult<PeopleDto>
        {
            Items = _mapper.Map<List<PeopleDto>>(pagedEntities.Items),
            Page = pagedEntities.Page,
            PageSize = pagedEntities.PageSize,
            TotalCount = pagedEntities.TotalCount
        };
        return Ok(pagedDto);
    }

    /// <summary>Obtiene un registro por ID.</summary>
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
        var entityObj = _mapper.Map<People>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()
            ?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))
            ?.GetValue(created);
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
