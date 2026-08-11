using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Overtime;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("overtime")]
public class OvertimeController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA", "Supervisor" };
    private const string ImmediateBossRole = "R_JEFE_INMEDIATO";

    private readonly IOvertimeService _svc;
    private readonly IEmployeesService _employeesSvc;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public OvertimeController(
        IOvertimeService svc,
        IEmployeesService employeesSvc,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _svc = svc;
        _employeesSvc = employeesSvc;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <summary>true si el usuario es el jefe inmediato asignado de employeeId (rol + ImmediateBossId real, no basta el rol).</summary>
    private async Task<bool> IsDirectBossOfAsync(int employeeId, CancellationToken ct)
    {
        if (!User.IsInRole(ImmediateBossRole) || _currentUser.EmployeeId is null)
            return false;

        var employee = await _employeesSvc.GetByIdAsync(employeeId, ct);
        return employee is not null && employee.ImmediateBossId == _currentUser.EmployeeId;
    }

    /// <summary>Lista todos los registros de Overtime. Solo RRHH/administración.</summary>
    [HttpGet]
    [RequirePermission("OVERTIME.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver horas extra de todos los empleados.");

        return Ok(_mapper.Map<List<OvertimeDto>>(await _svc.GetAllAsync(ct)));
    }

    /// <summary>Obtiene un registro por ID. El empleado puede ver las suyas (solo lectura); su jefe inmediato real o rol elevado pueden ver cualquiera.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("OVERTIME.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        if (e is null) return NotFound();

        var isOwner = _currentUser.EmployeeId == e.EmployeeId;
        if (!isOwner && !ElevatedRoles.Any(User.IsInRole) && !await IsDirectBossOfAsync(e.EmployeeId, ct))
            return Forbid403("No puede consultar horas extra de otro empleado.");

        return Ok(_mapper.Map<OvertimeDto>(e));
    }

    /// <summary>
    /// Crea un registro para un subordinado. Las horas extra se planifican por el jefe
    /// inmediato (o RRHH); el pipeline de asistencia las pasa después a "ejecutado" según
    /// las marcaciones reales. El propio empleado nunca crea sus horas extra, solo las
    /// consulta (GetById). ApprovedBy/SecondApprover del payload se ignoran.
    /// </summary>
    [HttpPost]
    [RequirePermission("OVERTIME.CREATE")]
    public async Task<IActionResult> Create([FromBody] OvertimeCreateDto dto, CancellationToken ct)
    {
        var isElevated = ElevatedRoles.Any(User.IsInRole);
        if (!isElevated && !await IsDirectBossOfAsync(dto.EmployeeId, ct))
            return Forbid403("Solo el jefe inmediato del empleado o RRHH puede planificar horas extra.");

        var entityObj = _mapper.Map<Overtime>(dto);
        entityObj.ApprovedBy = null;
        entityObj.SecondApprover = null;

        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<OvertimeDto>(created));
    }

    /// <summary>
    /// Actualiza un registro existente (ej. pasar de planificado a ejecutado). Solo el jefe
    /// inmediato real del empleado (rol R_JEFE_INMEDIATO + ImmediateBossId coincide) o rol
    /// elevado (RRHH/administración) — el propio empleado nunca edita sus horas extra, solo
    /// las consulta (GetById).
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("OVERTIME.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] OvertimeUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        var isElevated = ElevatedRoles.Any(User.IsInRole);
        var isDirectBoss = !isElevated && await IsDirectBossOfAsync(current.EmployeeId, ct);

        if (!isElevated && !isDirectBoss)
            return Forbid403("No puede editar horas extra de otro empleado.");

        var wasApproved = current.Status?.Contains("APPROV", StringComparison.OrdinalIgnoreCase) == true;
        var willBeApproved = dto.Status?.Contains("APPROV", StringComparison.OrdinalIgnoreCase) == true;

        var entityObj = _mapper.Map<Overtime>(dto);
        entityObj.EmployeeId = current.EmployeeId; // EmployeeId nunca cambia por esta vía

        if (willBeApproved && !wasApproved)
            entityObj.ApprovedBy = _currentUser.EmployeeId;

        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID. Solo jefe inmediato real o rol elevado — nunca el propio empleado.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("OVERTIME.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        var isElevated = ElevatedRoles.Any(User.IsInRole);
        if (!isElevated && !await IsDirectBossOfAsync(current.EmployeeId, ct))
            return Forbid403("No puede eliminar horas extra de otro empleado.");

        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}
