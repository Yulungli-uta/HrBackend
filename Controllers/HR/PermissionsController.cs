using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Permissions;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionsService _svc;
    private readonly IMapper _mapper;

    public PermissionsController(IPermissionsService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    /// <summary>Lista todos los permisos.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<PermissionsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Retorna un resultado paginado de permisos.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
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

        System.Linq.Expressions.Expression<Func<Permissions, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            predicate = p => (p.Justification != null && p.Justification.ToLower().Contains(term)) || p.Status.ToLower().Contains(term);
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
    }

    /// <summary>Obtiene un permiso por ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<PermissionsDto>(e));
    }

    /// <summary>Obtiene permisos por ID de empleado.</summary>
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmplopyeeId([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByEmployeeId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    /// <summary>Obtiene permisos por ID del jefe inmediato.</summary>
    [HttpGet("bossId/{employeeId:int}")]
    public async Task<IActionResult> GetByImmediateBossId([FromRoute] int employeeId, CancellationToken ct)
    {
        //var e = await _svc.GetByImmediateBossId(employeeId, ct);
        var e = await _svc.GetByImmediateBossIdNonMedical(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    /// <summary>Obtiene permisos NO médicos por ID del jefe inmediato.</summary>
    [HttpGet("bossId/{employeeId:int}/non-medical")]
    public async Task<IActionResult> GetByImmediateBossIdNonMedical([FromRoute] int employeeId, CancellationToken ct)
    {
        var e = await _svc.GetByImmediateBossIdNonMedical(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    /// <summary>Obtiene todos los permisos médicos pendientes.</summary>
    [HttpGet("medical/pending")]
    public async Task<IActionResult> GetPendingMedicalPermissions(CancellationToken ct)
    {
        var e = await _svc.GetPendingMedicalPermissions(ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    /// <summary>Crea un nuevo permiso con verificación de saldo.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PermissionsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Permissions>(dto);
        var created = await _svc.CreateWithBalanceCheckAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()
            ?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))
            ?.GetValue(created);

        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PermissionsDto>(created));
    }

    /// <summary>Actualiza un permiso existente afectando el saldo.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PermissionsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Permissions>(dto);
        await _svc.UpdateBalanceAffectAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un permiso por ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}