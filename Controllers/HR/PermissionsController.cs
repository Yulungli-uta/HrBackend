using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Permissions;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("permissions")]
public class PermissionsController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA", "Supervisor" };
    private const string ImmediateBossRole = "R_JEFE_INMEDIATO";

    private readonly IPermissionsService _svc;
    private readonly IEmployeesService _employeesSvc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public PermissionsController(
        IPermissionsService svc,
        IEmployeesService employeesSvc,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _svc = svc;
        _employeesSvc = employeesSvc;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    /// <summary>
    /// true si el usuario autenticado es específicamente el jefe inmediato asignado del
    /// dueño de este permiso (rol R_JEFE_INMEDIATO + Employees.ImmediateBossId == quien
    /// llama). No decide médico vs no-médico — eso lo sigue resolviendo
    /// PermissionsService.EnsureUpdatePermissionAsync con el código de permiso adecuado.
    /// </summary>
    private async Task<bool> IsDirectBossOfAsync(int employeeId, CancellationToken ct)
    {
        if (!User.IsInRole(ImmediateBossRole) || _currentUser.EmployeeId is null)
            return false;

        var employee = await _employeesSvc.GetByIdAsync(employeeId, ct);
        return employee is not null && employee.ImmediateBossId == _currentUser.EmployeeId;
    }

    /// <summary>Lista todos los permisos.</summary>
    [HttpGet]
    [RequirePermission("PERMISSIONS_LICENSES.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<PermissionsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Retorna un resultado paginado de permisos.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
    /// <param name="sortBy">Campo de ordenamiento (opcional).</param>
    /// <param name="sortDirection">Dirección del orden: asc | desc (opcional).</param>
    [HttpGet("paged")]
    [RequirePermission("PERMISSIONS_LICENSES.READ")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "desc",
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

        // sortBy/sortDirection antes se recibían pero nunca se aplicaban — el resultado no
        // tenía ningún orden real. Por defecto: fecha de registro descendente (más reciente primero).
        System.Linq.Expressions.Expression<Func<Permissions, object>> orderBy = sortBy?.Trim().ToLowerInvariant() switch
        {
            "startdate" => p => p.StartDate,
            "enddate" => p => p.EndDate,
            "status" => p => p.Status,
            _ => p => p.CreatedAt!
        };
        var ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        var pagedEntities = predicate is not null
            ? await _svc.GetPagedAsync(predicate, page, pageSize, ct, orderBy, ascending)
            : await _svc.GetPagedAsync(page, pageSize, ct, orderBy, ascending);

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
    [RequirePermission("PERMISSIONS_LICENSES.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        if (e is null) return NotFound();

        var isOwner = _currentUser.EmployeeId == e.EmployeeId;
        var isElevated = ElevatedRoles.Any(User.IsInRole);
        var isDirectBoss = !isOwner && !isElevated && await IsDirectBossOfAsync(e.EmployeeId, ct);

        if (!isOwner && !isElevated && !isDirectBoss)
            return Forbid403("No puede consultar permisos de otro empleado.");

        return Ok(_mapper.Map<PermissionsDto>(e));
    }

    /// <summary>Obtiene permisos por ID de empleado.</summary>
    [HttpGet("employee/{employeeId:int}")]
    [RequirePermission("PERMISSIONS_LICENSES.READ")]
    public async Task<IActionResult> GetByEmplopyeeId([FromRoute] int employeeId, CancellationToken ct)
    {
        if (_currentUser.EmployeeId != employeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede consultar permisos de otro empleado.");

        var e = await _svc.GetByEmployeeId(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    /// <summary>Obtiene permisos por ID del jefe inmediato.</summary>
    [HttpGet("bossId/{employeeId:int}")]
    [RequirePermission("PERMISSIONS_LICENSES.READ")]
    public async Task<IActionResult> GetByImmediateBossId([FromRoute] int employeeId, CancellationToken ct)
    {
        if (_currentUser.EmployeeId != employeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede consultar el equipo de otro jefe.");

        //var e = await _svc.GetByImmediateBossId(employeeId, ct);
        var e = await _svc.GetByImmediateBossIdNonMedical(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    /// <summary>Obtiene permisos NO médicos por ID del jefe inmediato.</summary>
    [HttpGet("bossId/{employeeId:int}/non-medical")]
    [RequirePermission("PERMISSIONS_LICENSES.READ")]
    public async Task<IActionResult> GetByImmediateBossIdNonMedical([FromRoute] int employeeId, CancellationToken ct)
    {
        if (_currentUser.EmployeeId != employeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede consultar el equipo de otro jefe.");

        var e = await _svc.GetByImmediateBossIdNonMedical(employeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    /// <summary>Obtiene todos los permisos médicos pendientes.</summary>
    [HttpGet("medical/pending")]
    [RequirePermission("PERMISSIONS_LICENSES.READ")]
    public async Task<IActionResult> GetPendingMedicalPermissions(CancellationToken ct)
    {
        var e = await _svc.GetPendingMedicalPermissions(ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PermissionsDto>>(e));
    }

    /// <summary>
    /// Crea un nuevo permiso con verificación de saldo, para el empleado autenticado.
    /// EmployeeId/Status/ApprovedBy/ApprovedAt del payload se ignoran deliberadamente —
    /// mismo criterio que VacationsController.
    /// </summary>
    [HttpPost]
    [RequirePermission("PERMISSIONS_LICENSES.CREATE")]
    public async Task<IActionResult> Create([FromBody] PermissionsCreateDto dto, CancellationToken ct)
    {
        if (_currentUser.EmployeeId is null)
            return Forbid403("No se pudo determinar el empleado autenticado.");

        var entityObj = _mapper.Map<Permissions>(dto);
        entityObj.EmployeeId = _currentUser.EmployeeId.Value;
        entityObj.Status = "Pending";
        entityObj.ApprovedBy = null;
        entityObj.ApprovedAt = null;

        var created = await _svc.CreateWithBalanceCheckAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()
            ?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))
            ?.GetValue(created);

        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PermissionsDto>(created));
    }

    /// <summary>
    /// Actualiza un permiso existente afectando el saldo. Permitido para: el propio dueño,
    /// roles de RRHH/administración (ElevatedRoles), o el jefe inmediato REAL de ese
    /// empleado (rol R_JEFE_INMEDIATO + ImmediateBossId coincide). La distinción médico/
    /// no-médico y la auto-aprobación las sigue resolviendo
    /// PermissionsService.EnsureUpdatePermissionAsync/UpdateBalanceAffectAsync, sin cambios.
    /// NOTA preexistente: el catálogo de permisos de acción no tiene un código
    /// PERMISSIONS_LICENSES.UPDATE dedicado (solo READ/CREATE/APPROVE/REJECT) — este
    /// endpoint mezcla edición general y aprobación/rechazo; sigue protegido por el chequeo
    /// de propiedad/rol de abajo, no por [RequirePermission] estático.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PermissionsUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        var isOwner = _currentUser.EmployeeId == current.EmployeeId;
        var isElevated = ElevatedRoles.Any(User.IsInRole);
        var isDirectBoss = !isOwner && !isElevated && await IsDirectBossOfAsync(current.EmployeeId, ct);

        if (!isOwner && !isElevated && !isDirectBoss)
            return Forbid403("No puede editar permisos de otro empleado.");

        var entityObj = _mapper.Map<Permissions>(dto);
        await _svc.UpdateBalanceAffectAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un permiso por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("PERMISSIONS_LICENSES.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (_currentUser.EmployeeId != current.EmployeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede eliminar permisos de otro empleado.");

        // Libera la reserva de saldo activa (si hay) antes de borrar — el DeleteAsync
        // genérico no lo hacía, dejando el saldo descontado para siempre.
        await _svc.DeleteWithBalanceReleaseAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}
