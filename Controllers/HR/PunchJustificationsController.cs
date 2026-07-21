using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.PunchJustifications;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("attendance/punch-justifications")]
public class PunchJustificationsController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA", "Supervisor" };

    private readonly IPunchJustificationsService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    public PunchJustificationsController(IPunchJustificationsService svc, IMapper mapper, ICurrentUserService currentUser)
    { _svc = svc; _mapper = mapper; _currentUser = currentUser; }

    /// <summary>Lista todos los registros de PunchJustifications. Requiere rol de RRHH/administración.</summary>
    [HttpGet]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver todas las justificaciones.");

        return Ok(_mapper.Map<List<PunchJustificationsDto>>(await _svc.GetAllAsync(ct)));
    }

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        if (e is null) return NotFound();

        if (_currentUser.EmployeeId != e.EmployeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede consultar justificaciones de otro empleado.");

        return Ok(_mapper.Map<PunchJustificationsDto>(e));
    }

    [HttpGet("bossId/{BossEmployeeId:int}")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetByBossEmployeeId([FromRoute] int BossEmployeeId, CancellationToken ct)
    {
        if (_currentUser.EmployeeId != BossEmployeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede consultar el equipo de otro jefe.");

        var e = await _svc.GetByBossEmployeeId(BossEmployeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<PunchJustificationsDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("ATTENDANCE.CREATE")]
    public async Task<IActionResult> Create([FromBody] PunchJustificationsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<PunchJustifications>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PunchJustificationsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("ATTENDANCE.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PunchJustificationsUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (_currentUser.EmployeeId != current.EmployeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede editar justificaciones de otro empleado.");

        var entityObj = _mapper.Map<PunchJustifications>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("ATTENDANCE.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (_currentUser.EmployeeId != current.EmployeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede eliminar justificaciones de otro empleado.");

        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}
