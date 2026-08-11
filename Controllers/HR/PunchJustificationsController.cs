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
    private const string ImmediateBossRole = "R_JEFE_INMEDIATO";

    private readonly IPunchJustificationsService _svc;
    private readonly IEmployeesService _employeesSvc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public PunchJustificationsController(
        IPunchJustificationsService svc,
        IEmployeesService employeesSvc,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _svc = svc;
        _employeesSvc = employeesSvc;
        _mapper = mapper;
        _currentUser = currentUser;
    }

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

        if (_currentUser.EmployeeId != e.EmployeeId && _currentUser.EmployeeId != e.BossEmployeeId && !ElevatedRoles.Any(User.IsInRole))
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

    /// <summary>
    /// Crea una justificación para el empleado autenticado. EmployeeId se resuelve del
    /// usuario autenticado; BossEmployeeId se resuelve de Employees.ImmediateBossId (nunca
    /// del payload) para que el aprobador quede fijado a quien realmente corresponde, no a
    /// quien el cliente decida enviar. Approved/ApprovedAt siempre arrancan en false/null.
    /// </summary>
    [HttpPost]
    [RequirePermission("ATTENDANCE.CREATE")]
    public async Task<IActionResult> Create([FromBody] PunchJustificationsCreateDto dto, CancellationToken ct)
    {
        if (_currentUser.EmployeeId is null)
            return Forbid403("No se pudo determinar el empleado autenticado.");

        var employee = await _employeesSvc.GetByIdAsync(_currentUser.EmployeeId.Value, ct);
        if (employee?.ImmediateBossId is null)
            return BadRequest(new { status = "error", message = "El empleado autenticado no tiene jefe inmediato asignado." });

        var entityObj = _mapper.Map<PunchJustifications>(dto);
        entityObj.EmployeeId = _currentUser.EmployeeId.Value;
        entityObj.BossEmployeeId = employee.ImmediateBossId.Value;
        entityObj.Approved = false;
        entityObj.ApprovedAt = null;
        entityObj.Status = "PENDING";

        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PunchJustificationsDto>(created));
    }

    /// <summary>
    /// Actualiza un registro existente. El propio dueño puede editar mientras siga
    /// PENDING, pero nunca puede aprobar/rechazar su propia justificación — eso es
    /// exclusivo de su jefe inmediato real (BossEmployeeId de este registro coincide con
    /// quien llama, rol R_JEFE_INMEDIATO) o de RRHH/administración.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("ATTENDANCE.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PunchJustificationsUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        var isOwner = _currentUser.EmployeeId == current.EmployeeId;
        var isElevated = ElevatedRoles.Any(User.IsInRole);
        var isDirectBoss = !isOwner && !isElevated
            && User.IsInRole(ImmediateBossRole)
            && _currentUser.EmployeeId == current.BossEmployeeId;

        if (!isOwner && !isElevated && !isDirectBoss)
            return Forbid403("No puede editar justificaciones de otro empleado.");

        var wasDecided = current.Status?.Contains("APPROV", StringComparison.OrdinalIgnoreCase) == true
            || current.Status?.Contains("REJECT", StringComparison.OrdinalIgnoreCase) == true
            || current.Approved;
        var willBeDecided = dto.Approved
            || dto.Status?.Contains("APPROV", StringComparison.OrdinalIgnoreCase) == true
            || dto.Status?.Contains("REJECT", StringComparison.OrdinalIgnoreCase) == true;

        if (isOwner && !isElevated && !isDirectBoss && willBeDecided && !wasDecided)
            return Forbid403("No puede aprobar o rechazar su propia justificación.");

        var entityObj = _mapper.Map<PunchJustifications>(dto);
        entityObj.EmployeeId = current.EmployeeId; // EmployeeId/BossEmployeeId nunca cambian por esta vía
        entityObj.BossEmployeeId = current.BossEmployeeId;

        if (willBeDecided && !wasDecided)
            entityObj.ApprovedAt = DateTime.Now;

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
