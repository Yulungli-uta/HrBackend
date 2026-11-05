using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.TeacherStructure;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Personal docente: listado y conteos de titulares (categoría, nivel, dedicación, sueldo y nombramiento)
/// y de no titulares (docentes ocasionales: dedicación, tipo y fechas de contrato).
/// </summary>
[ApiController]
[Route("teacher-roster")]
public class TeacherRosterController : ControllerBase
{
    private const string SalaryPermission = "SALARY_HISTORY.READ";

    private readonly ITeacherRosterService _svc;
    private readonly INonTitularRosterService _nonTitularSvc;
    private readonly IUserActionPermissionService _permissions;

    public TeacherRosterController(
        ITeacherRosterService svc, INonTitularRosterService nonTitularSvc, IUserActionPermissionService permissions)
    {
        _svc = svc;
        _nonTitularSvc = nonTitularSvc;
        _permissions = permissions;
    }

    /// <summary>Listado paginado con filtros. El RMU solo viaja si el usuario tiene SALARY_HISTORY.READ.</summary>
    [HttpGet]
    [RequirePermission("EMPLOYEES.READ")]
    public async Task<IActionResult> GetPaged([FromQuery] TeacherRosterFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetPagedAsync(filter, await CanViewSalaryAsync(ct), ct));

    /// <summary>Conteos para las tarjetas (total, por categoría, nivel, dedicación y categoría × dedicación).</summary>
    [HttpGet("summary")]
    [RequirePermission("EMPLOYEES.READ")]
    public async Task<IActionResult> GetSummary([FromQuery] TeacherRosterFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetSummaryAsync(filter, ct));

    /// <summary>Docentes no titulares (contrato ocasional) paginados. El RMU solo viaja con SALARY_HISTORY.READ.</summary>
    [HttpGet("non-titular")]
    [RequirePermission("EMPLOYEES.READ")]
    public async Task<IActionResult> GetNonTitularPaged([FromQuery] NonTitularRosterFilterDto filter, CancellationToken ct) =>
        Ok(await _nonTitularSvc.GetPagedAsync(filter, await CanViewSalaryAsync(ct), ct));

    /// <summary>Conteos de las tarjetas de no titulares (total, por dedicación y por tipo de contrato).</summary>
    [HttpGet("non-titular/summary")]
    [RequirePermission("EMPLOYEES.READ")]
    public async Task<IActionResult> GetNonTitularSummary([FromQuery] NonTitularRosterFilterDto filter, CancellationToken ct) =>
        Ok(await _nonTitularSvc.GetSummaryAsync(filter, ct));

    /// <summary>
    /// Verificado en el servidor contra los roles reales del token, nunca un indicador enviado por el cliente.
    /// A diferencia de <see cref="RequirePermissionAttribute"/> (modo sombra), aquí un resultado negativo SÍ oculta el dato.
    /// </summary>
    private async Task<bool> CanViewSalaryAsync(CancellationToken ct)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        return await _permissions.HasPermissionAsync(roles, SalaryPermission, ct);
    }
}
