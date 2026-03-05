using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Vacations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("vacations")]
public class VacationsController : ControllerBase
{
    private readonly IVacationsService _svc;
    private readonly IMapper _mapper;

    public VacationsController(IVacationsService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    /// <summary>Lista todas las vacaciones.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<VacationsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Retorna un resultado paginado de vacaciones.</summary>
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

        System.Linq.Expressions.Expression<Func<Vacations, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            predicate = v => v.Status.ToLower().Contains(term);
        }

        var pagedEntities = predicate is not null
            ? await _svc.GetPagedAsync(predicate, page, pageSize, ct)
            : await _svc.GetPagedAsync(page, pageSize, ct);

        return Ok(new
        {
            items = pagedEntities.Items,
            page = pagedEntities.Page,
            pageSize = pagedEntities.PageSize,
            totalCount = pagedEntities.TotalCount,
            totalPages = pagedEntities.TotalPages,
            hasPreviousPage = pagedEntities.HasPreviousPage,
            hasNextPage = pagedEntities.HasNextPage
        });
    };
        return Ok(pagedDto);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<VacationsDto>(e));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByEmployeeId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<VacationsDto>>(e));
    }

    [HttpGet("bossId/{employeeId:int}")]
    public async Task<IActionResult> GetByImmediateBossId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByImmediateBossId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<VacationsDto>>(e));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VacationsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Vacations>(dto);
        var created = await _svc.CreateWithBalanceCheckAsync(entityObj, ct);

        var idVal = created?.GetType()?.GetProperties()
            ?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))
            ?.GetValue(created);

        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<VacationsDto>(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] VacationsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Vacations>(dto);

        // ✅ Antes: UpdateAsync (no afectaba saldo)
        // ✅ Ahora: UpdateBalanceAffectAsync (sí afecta saldo)
        await _svc.UpdateBalanceAffectAsync(id, entityObj, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
