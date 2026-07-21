using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.PermissionTypes;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("permission-types")]
public class PermissionTypesController : ControllerBase
{
    private readonly IPermissionTypesService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public PermissionTypesController(
        IPermissionTypesService svc,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _svc = svc;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    /// <summary>Lista todos los registros de PermissionTypes.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<PermissionTypesDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>
    /// Retorna los tipos de permiso activos disponibles para TODOS los regímenes laborales
    /// activos del empleado actualmente autenticado (incluye los de régimen NULL = todos).
    /// Fuente: HR.tbl_EmployeeLaborRegime — un empleado con más de un régimen activo
    /// (ej. nombramiento LOSEP + contrato LOES) ve los tipos de permiso de ambos.
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(CancellationToken ct)
    {
        var employeeId = _currentUser.EmployeeId;
        if (employeeId is null)
            return Ok(_mapper.Map<List<PermissionTypesDto>>(await _svc.GetAllAsync(ct)));

        var items = await _svc.GetAvailableForEmployeeAsync(employeeId.Value, ct);
        return Ok(_mapper.Map<List<PermissionTypesDto>>(items));
    }

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<PermissionTypesDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    public async Task<IActionResult> Create([FromBody] PermissionTypesCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<PermissionTypes>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        //Console.WriteLine($"Created entity: {created} dto: {dto}");
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PermissionTypesDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PermissionTypesUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<PermissionTypes>(dto);
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
