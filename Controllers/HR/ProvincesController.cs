using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.Provinces;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("geo/provinces")]
public class ProvincesController : ControllerBase
{
    private readonly IProvincesService _svc;
    private readonly IMapper _mapper;
    public ProvincesController(IProvincesService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de Provinces.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ProvincesDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Lista paginada de provincias con búsqueda por nombre y filtro opcional por país.</summary>
    /// <param name="page">Número de página (1-based).</param>
    /// <param name="pageSize">Registros por página. Máximo 200.</param>
    /// <param name="search">Texto a buscar en el nombre.</param>
    /// <param name="countryId">Limita a las provincias de este país.</param>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? countryId = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();
        var country = string.IsNullOrWhiteSpace(countryId) ? null : countryId.Trim();
        System.Linq.Expressions.Expression<Func<Provinces, bool>>? predicate = (term, country) switch
        {
            (null, null) => null,
            (not null, null) => p => p.ProvinceName.ToLower().Contains(term),
            (null, not null) => p => p.CountryId == country,
            _ => p => p.CountryId == country && p.ProvinceName.ToLower().Contains(term!)
        };

        var paged = await _svc.GetPagedAsync(predicate, page, pageSize, ct, p => p.ProvinceName);
        return Ok(new
        {
            items = _mapper.Map<List<ProvincesDto>>(paged.Items),
            page = paged.Page,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount,
            totalPages = paged.TotalPages,
            hasPreviousPage = paged.HasPreviousPage,
            hasNextPage = paged.HasNextPage
        });
    }

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<ProvincesDto>(e));
    }

    /// <summary>Obtiene todas las provincias de un país.</summary>
    /// <param name="countryId">ID del país</param>
    [HttpGet("country/{countryId}")]
    public async Task<IActionResult> GetByCountryId([FromRoute] string countryId, CancellationToken ct)
    {
        var provinces = await _svc.GetByCountryIdAsync(countryId);
        return Ok(_mapper.Map<List<ProvincesDto>>(provinces));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    public async Task<IActionResult> Create([FromBody] ProvincesCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Provinces>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<ProvincesDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] string id, [FromBody] ProvincesUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Provinces>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("CATALOGS.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
