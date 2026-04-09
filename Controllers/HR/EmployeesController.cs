using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using WsUtaSystem.Application.DTOs.Employees;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeesService _svc;
    private readonly IMapper _mapper;
    public EmployeesController(IEmployeesService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de Employees.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<EmployeesDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>
    /// Retorna un resultado paginado de registros de Employees.
    /// </summary>
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
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        Expression<Func<Employees, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            predicate = e =>
                (e.Email != null && e.Email.ToLower().Contains(term)) ||
                e.EmployeeId.ToString().Contains(term) ||
                e.PersonID.ToString().Contains(term) ||
                e.EmployeeType.ToString().Contains(term) ||
                (e.DepartmentId.HasValue && e.DepartmentId.Value.ToString().Contains(term)) ||
                (e.ImmediateBossId.HasValue && e.ImmediateBossId.Value.ToString().Contains(term));
        }

        var paged = predicate is null
            ? await _svc.GetPagedAsync(page, pageSize, ct)
            : await _svc.GetPagedAsync(predicate, page, pageSize, ct);

        return Ok(new
        {
            items = _mapper.Map<List<EmployeesDto>>(paged.Items),
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
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<EmployeesDto>(e));
    }

    [HttpGet("boss/{bossId:int}/subordinates")]
    public async Task<IActionResult> GetSubordinatesByBossId(
        int bossId,
        CancellationToken ct)
    {
        var result = await _svc.GetSubordinatesByBossIdAsync(bossId, ct);
        return Ok(result);
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeesCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Employees>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<EmployeesDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EmployeesUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Employees>(dto);
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
