using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.Cantons;
using WsUtaSystem.Models;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("geo/cantons")]
public class CantonsController : ControllerBase
{
    private readonly ICantonsService _svc;
    private readonly IMapper _mapper;
    public CantonsController(ICantonsService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>
    /// Ya no lista todos los cantones: el catálogo mundial tiene decenas de miles de filas. Use
    /// <c>geo/cantons/province/{provinceId}</c> (cantones de una provincia) o <c>geo/cantons/paged</c> (búsqueda paginada).
    /// </summary>
    [HttpGet]
    public IActionResult GetAll() =>
        BadRequest(new
        {
            message = "El catálogo de cantones es muy grande para listarlo completo. Consulte por provincia (geo/cantons/province/{provinceId}) o use la búsqueda paginada (geo/cantons/paged)."
        });

    /// <summary>Lista paginada de cantones con búsqueda por nombre y filtro opcional por provincia.</summary>
    /// <param name="page">Número de página (1-based).</param>
    /// <param name="pageSize">Registros por página. Máximo 200.</param>
    /// <param name="search">Texto a buscar en el nombre.</param>
    /// <param name="provinceId">Limita a los cantones de esta provincia.</param>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? provinceId = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();
        var province = string.IsNullOrWhiteSpace(provinceId) ? null : provinceId.Trim();
        System.Linq.Expressions.Expression<Func<Cantons, bool>>? predicate = (term, province) switch
        {
            (null, null) => null,
            (not null, null) => c => c.CantonName.ToLower().Contains(term),
            (null, not null) => c => c.ProvinceId == province,
            _ => c => c.ProvinceId == province && c.CantonName.ToLower().Contains(term!)
        };

        var paged = await _svc.GetPagedAsync(predicate, page, pageSize, ct, c => c.CantonName);
        return Ok(new
        {
            items = _mapper.Map<List<CantonsDto>>(paged.Items),
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
        return e is null ? NotFound() : Ok(_mapper.Map<CantonsDto>(e));
    }

    /// <summary>Obtiene todos los cantones de una provincia.</summary>
    /// <param name="provinceId">ID de la provincia</param>
    [HttpGet("province/{provinceId}")]
    public async Task<IActionResult> GetByProvinceId([FromRoute] string provinceId, CancellationToken ct)
    {
        var cantons = await _svc.GetByProvinceIdAsync(provinceId);
        return Ok(_mapper.Map<List<CantonsDto>>(cantons));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    public async Task<IActionResult> Create([FromBody] CantonsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Cantons>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<CantonsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] string id, [FromBody] CantonsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Cantons>(dto);
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
