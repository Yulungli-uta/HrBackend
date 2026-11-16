using AutoMapper;
using WsUtaSystem.Application.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.Activity;
using WsUtaSystem.Models;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;


[ApiController]
[Route("activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _svc;
    private readonly IMapper _mapper;
    public ActivityController(IActivityService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de Activity.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ActivityDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>
    /// Listado paginado de actividades. Busca por palabras sin importar mayúsculas ni tildes y permite filtrar por estado y tipo
    /// (LABORAL / ADICIONAL).
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? type = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken ct = default)
    {
        (page, pageSize) = CatalogPredicates.NormalizePaging(page, pageSize);

        // Description es columna text (heredada): se compara/ordena sobre su inicio porque text no admite ORDER BY ni COLLATE.
        var predicate = CatalogPredicates.ByWords<Activity>(search,
            w => a => a.Description != null && EF.Functions.Collate(a.Description.Substring(0, 4000), CatalogPredicates.Collation).Contains(w));
        Expression<Func<Activity, bool>>? byState = isActive.HasValue ? a => a.IsActive == isActive.Value : null;
        var typeValue = string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToUpperInvariant();
        Expression<Func<Activity, bool>>? byType = typeValue is null ? null : a => a.ActivitiesType == typeValue;

        var paged = await _svc.GetPagedAsync(predicate.And(byState).And(byType), page, pageSize, ct,
            orderBy: a => a.Description!.Substring(0, 400), ascending: !string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase));
        return Ok(CatalogPredicates.ToResponse(paged, _mapper.Map<List<ActivityDto>>(paged.Items)));
    }

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<ActivityDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    public async Task<IActionResult> Create([FromBody] ActivityCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Activity>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<ActivityDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ActivityUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Activity>(dto);
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
