using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Controlador de solo lectura para la vista HR.vw_Authority.
/// Expone datos desnormalizados de autoridades de departamento con joins ya resueltos.
/// Ruta base: /api/v1/rh/vw-authority
/// </summary>
[ApiController]
[Route("vw-authority")]
[Produces("application/json")]
public sealed class VwAuthorityController : ControllerBase
{
    private readonly IVwAuthorityService _svc;

    public VwAuthorityController(IVwAuthorityService svc) => _svc = svc;

    // ─────────────────────────────────────────────────────────────────────────
    // GET — Listados
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna todos los registros de la vista sin paginación.
    /// Usar solo para catálogos pequeños; preferir /paged para listados grandes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    /// <summary>
    /// Retorna únicamente las autoridades activas y vigentes (IsActive = true y EndDate IS NULL).
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken ct) =>
        Ok(await _svc.GetActiveAsync(ct));

    /// <summary>
    /// Retorna un resultado paginado con búsqueda de texto libre.
    /// Busca en: nombre del empleado, cédula, nombre/código del departamento, tipo de autoridad, denominación.
    /// </summary>
    /// <param name="page">Número de página (base 1). Por defecto: 1.</param>
    /// <param name="pageSize">Registros por página (máx. 200). Por defecto: 20.</param>
    /// <param name="search">Texto de búsqueda libre (opcional).</param>
    /// <param name="onlyActive">Si true, retorna solo registros activos y vigentes.</param>
    [HttpGet("paged")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool onlyActive = false,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var paged = await _svc.GetPagedAsync(search, page, pageSize, onlyActive, ct);

        return Ok(new
        {
            items = paged.Items,
            page = paged.Page,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount,
            totalPages = paged.TotalPages,
            hasPreviousPage = paged.HasPreviousPage,
            hasNextPage = paged.HasNextPage
        });
    }

    /// <summary>
    /// Retorna las autoridades de un departamento específico ordenadas por fecha de inicio descendente.
    /// </summary>
    /// <param name="departmentId">ID del departamento.</param>
    [HttpGet("by-department/{departmentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByDepartment(
        [FromRoute] int departmentId,
        CancellationToken ct)
    {
        if (departmentId <= 0)
            return BadRequest(new { message = "El ID del departamento debe ser un valor positivo." });

        return Ok(await _svc.GetByDepartmentAsync(departmentId, ct));
    }

    /// <summary>
    /// Retorna el historial de autoridades de un empleado ordenado por fecha de inicio descendente.
    /// </summary>
    /// <param name="employeeId">ID del empleado.</param>
    [HttpGet("by-employee/{employeeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByEmployee(
        [FromRoute] int employeeId,
        CancellationToken ct)
    {
        if (employeeId <= 0)
            return BadRequest(new { message = "El ID del empleado debe ser un valor positivo." });

        return Ok(await _svc.GetByEmployeeAsync(employeeId, ct));
    }

    /// <summary>
    /// Retorna un registro por su AuthorityID.
    /// </summary>
    /// <param name="id">ID de la autoridad de departamento.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null
            ? NotFound(new { message = $"No se encontró la autoridad con ID {id}." })
            : Ok(result);
    }
}
