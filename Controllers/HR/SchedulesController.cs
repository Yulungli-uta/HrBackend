using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using WsUtaSystem.Application.DTOs.Schedules;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("schedules")]
public class SchedulesController : ControllerBase
{
    private readonly ISchedulesService _svc;
    private readonly IMapper _mapper;
    public SchedulesController(ISchedulesService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de Schedules.</summary>
    [HttpGet]
    [RequirePermission("SCHEDULES.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<SchedulesDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Lista horarios activos marcados como rotativos para el módulo de guardias.</summary>
    [HttpGet("rotating")]
    [RequirePermission("SCHEDULES.READ")]
    public async Task<IActionResult> GetRotating([FromQuery] string? search = null, CancellationToken ct = default)
    {
        var schedules = await _svc.GetBySheduleAcive(ct);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            schedules = schedules.Where(s =>
                (!string.IsNullOrWhiteSpace(s.Description) && s.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.ScheduleCode) && s.ScheduleCode.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.WorkingDays) && s.WorkingDays.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.RotationPattern) && s.RotationPattern.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        return Ok(_mapper.Map<List<SchedulesDto>>(schedules));
    }

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("SCHEDULES.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<SchedulesDto>(e));
    }

    [HttpGet("paged")]
    [RequirePermission("SCHEDULES.READ")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isRotating = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var term = search?.Trim().ToLower();
        var hasSearch = !string.IsNullOrWhiteSpace(term);

        Expression<Func<Schedules, bool>>? predicate = null;
        if (hasSearch || isRotating.HasValue)
        {
            predicate = s =>
                (!isRotating.HasValue || s.IsRotating == isRotating.Value) &&
                (!hasSearch ||
                 (s.Description != null && s.Description.ToLower().Contains(term!)) ||
                 (s.WorkingDays != null && s.WorkingDays.ToLower().Contains(term!)) ||
                 (s.RotationPattern != null && s.RotationPattern.ToLower().Contains(term!)) ||
                 (s.ScheduleCode != null && s.ScheduleCode.ToLower().Contains(term!)));
        }

        var pagedEntities = predicate is not null
            ? await _svc.GetPagedAsync(predicate, page, pageSize, ct)
            : await _svc.GetPagedAsync(page, pageSize, ct);

        var dtoItems = _mapper.Map<List<SchedulesDto>>(pagedEntities.Items);

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

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("SCHEDULES.CREATE")]
    public async Task<IActionResult> Create([FromBody] SchedulesCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Schedules>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<SchedulesDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("SCHEDULES.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SchedulesUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Schedules>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("SCHEDULES.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
