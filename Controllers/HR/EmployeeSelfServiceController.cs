using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Autoservicio del empleado: perfil, resumen, permisos/vacaciones propios e historial
/// consolidado. EmployeeId siempre resuelto desde <see cref="ICurrentUserService"/> —
/// ningún endpoint de este controller acepta un EmployeeId del frontend. A diferencia de
/// <c>GET /permissions/employee/{employeeId}</c> y <c>GET /vacations/employee/{employeeId}</c>
/// (que confían en el id de la ruta), estos endpoints "my" son la vía segura para el
/// autoservicio — no se modificaron los controllers existentes.
/// </summary>
[ApiController]
[Route("employee-self-service")]
public sealed class EmployeeSelfServiceController : ControllerBase
{
    private readonly IEmployeeSelfServiceService _selfService;
    private readonly IPermissionsService _permissionsService;
    private readonly IVacationsService _vacationsService;
    private readonly ICurrentUserService _currentUser;

    public EmployeeSelfServiceController(
        IEmployeeSelfServiceService selfService,
        IPermissionsService permissionsService,
        IVacationsService vacationsService,
        ICurrentUserService currentUser)
    {
        _selfService = selfService;
        _permissionsService = permissionsService;
        _vacationsService = vacationsService;
        _currentUser = currentUser;
    }

    private int RequireEmployeeId()
        => _currentUser.EmployeeId
           ?? throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => Ok(await _selfService.GetProfileAsync(RequireEmployeeId(), ct));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
        => Ok(await _selfService.GetSummaryAsync(RequireEmployeeId(), ct));

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
        => Ok(await _selfService.GetHistoryAsync(RequireEmployeeId(), ct));

    /// <summary>Mis permisos — resuelve el EmployeeId del token, no de la URL.</summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> GetMyPermissions(CancellationToken ct)
        => Ok(await _permissionsService.GetByEmployeeId(RequireEmployeeId(), ct));

    /// <summary>Mis vacaciones — resuelve el EmployeeId del token, no de la URL.</summary>
    [HttpGet("vacations")]
    public async Task<IActionResult> GetMyVacations(CancellationToken ct)
        => Ok(await _vacationsService.GetByEmployeeId(RequireEmployeeId(), ct));
}
