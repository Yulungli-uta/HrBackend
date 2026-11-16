using AutoMapper;
using WsUtaSystem.Application.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.OccupationalGroup;
using WsUtaSystem.Models;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;


[ApiController]
[Route("occupational-group")]
public class OccupationalGroupController : ControllerBase
{
    private readonly IOccupationalGroupService _svc;
    private readonly IMapper _mapper;
    public OccupationalGroupController(IOccupationalGroupService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de OccupationalGroup.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<OccupationalGroupDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>
    /// Listado paginado de grupos ocupacionales. Busca por palabras (descripción) sin importar mayúsculas ni tildes
    /// y permite filtrar por estado, grado, familia y rango de RMU.
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? degreeId = null,
        [FromQuery] int? familyTypeId = null,
        [FromQuery] decimal? rmuMin = null,
        [FromQuery] decimal? rmuMax = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken ct = default)
    {
        (page, pageSize) = CatalogPredicates.NormalizePaging(page, pageSize);

        var predicate = CatalogPredicates.ByWords<OccupationalGroup>(search,
            w => g => EF.Functions.Collate(g.Description, CatalogPredicates.Collation).Contains(w));
        Expression<Func<OccupationalGroup, bool>>? byState = isActive.HasValue ? g => g.IsActive == isActive.Value : null;
        Expression<Func<OccupationalGroup, bool>>? byDegree = degreeId.HasValue ? g => g.DegreeId == degreeId.Value : null;
        Expression<Func<OccupationalGroup, bool>>? byFamily = familyTypeId.HasValue ? g => g.FamilyTypeId == familyTypeId.Value : null;
        Expression<Func<OccupationalGroup, bool>>? byRmuMin = rmuMin.HasValue ? g => g.Rmu >= rmuMin.Value : null;
        Expression<Func<OccupationalGroup, bool>>? byRmuMax = rmuMax.HasValue ? g => g.Rmu <= rmuMax.Value : null;

        var filter = predicate.And(byState).And(byDegree).And(byFamily).And(byRmuMin).And(byRmuMax);
        var paged = await _svc.GetPagedAsync(filter, page, pageSize, ct,
            orderBy: g => g.Description, ascending: !string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase));
        return Ok(CatalogPredicates.ToResponse(paged, _mapper.Map<List<OccupationalGroupDto>>(paged.Items)));
    }

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<OccupationalGroupDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    public async Task<IActionResult> Create([FromBody] OccupationalGroupCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<OccupationalGroup>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<OccupationalGroupDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] OccupationalGroupUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<OccupationalGroup>(dto);
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
