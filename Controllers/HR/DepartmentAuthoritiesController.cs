using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.DepartmentAuthority;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Controlador REST para la gestión de autoridades de departamento.
/// Expone endpoints CRUD completos, paginación con búsqueda y consulta especializada por cédula.
/// Ruta base: /api/department-authorities
/// </summary>
[ApiController]
[Route("department-authorities")]
[Produces("application/json")]
public sealed class DepartmentAuthoritiesController : ControllerBase
{
    private readonly IDepartmentAuthorityService _svc;
    private readonly IMapper _mapper;

    public DepartmentAuthoritiesController(IDepartmentAuthorityService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET — Listados y paginación
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna todos los registros de autoridades de departamento sin paginación.
    /// Usar solo para catálogos pequeños; preferir /paged para listados grandes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DepartmentAuthorityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<DepartmentAuthorityDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>
    /// Retorna un resultado paginado con búsqueda de texto libre.
    /// Busca en: denominación, código de resolución, notas, nombre del departamento y nombre del empleado.
    /// </summary>
    /// <param name="page">Número de página (base 1). Por defecto: 1.</param>
    /// <param name="pageSize">Registros por página (máx. 200). Por defecto: 20.</param>
    /// <param name="search">Texto de búsqueda libre (opcional).</param>
    /// <param name="onlyActive">Si es true, retorna solo registros activos y vigentes (EndDate IS NULL).</param>
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

        var paged = await _svc.GetPagedWithSearchAsync(search, page, pageSize, ct, onlyActive);
        var dtoItems = _mapper.Map<List<DepartmentAuthorityDto>>(paged.Items);

        return Ok(new
        {
            items = dtoItems,
            page = paged.Page,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount,
            totalPages = paged.TotalPages,
            hasPreviousPage = paged.HasPreviousPage,
            hasNextPage = paged.HasNextPage
        });
    }

    /// <summary>
    /// Retorna un resultado paginado de autoridades filtradas por departamento.
    /// </summary>
    /// <param name="departmentId">ID del departamento a filtrar.</param>
    /// <param name="page">Número de página (base 1). Por defecto: 1.</param>
    /// <param name="pageSize">Registros por página (máx. 200). Por defecto: 20.</param>
    /// <param name="onlyActive">Si es true, retorna solo registros activos y vigentes.</param>
    [HttpGet("by-department/{departmentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByDepartment(
        [FromRoute] int departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool onlyActive = false,
        CancellationToken ct = default)
    {
        if (departmentId <= 0)
            return BadRequest(new { message = "El ID del departamento debe ser un valor positivo." });

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var paged = await _svc.GetPagedByDepartmentAsync(departmentId, page, pageSize, ct, onlyActive);
        var dtoItems = _mapper.Map<List<DepartmentAuthorityDto>>(paged.Items);

        return Ok(new
        {
            items = dtoItems,
            page = paged.Page,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount,
            totalPages = paged.TotalPages,
            hasPreviousPage = paged.HasPreviousPage,
            hasNextPage = paged.HasNextPage
        });
    }

    /// <summary>
    /// Retorna todas las autoridades activas de un departamento (sin paginación).
    /// Útil para mostrar el panel de autoridades vigentes de un departamento específico.
    /// </summary>
    /// <param name="departmentId">ID del departamento.</param>
    [HttpGet("by-department/{departmentId:int}/active")]
    [ProducesResponseType(typeof(IEnumerable<DepartmentAuthorityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetActiveByDepartment(
        [FromRoute] int departmentId,
        CancellationToken ct)
    {
        if (departmentId <= 0)
            return BadRequest(new { message = "El ID del departamento debe ser un valor positivo." });

        var items = await _svc.GetActiveByDepartmentAsync(departmentId, ct);
        return Ok(_mapper.Map<List<DepartmentAuthorityDto>>(items));
    }

    /// <summary>
    /// Retorna un resultado paginado de autoridades filtradas por empleado.
    /// </summary>
    /// <param name="employeeId">ID del empleado.</param>
    /// <param name="page">Número de página (base 1). Por defecto: 1.</param>
    /// <param name="pageSize">Registros por página (máx. 200). Por defecto: 20.</param>
    [HttpGet("by-employee/{employeeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByEmployee(
        [FromRoute] int employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (employeeId <= 0)
            return BadRequest(new { message = "El ID del empleado debe ser un valor positivo." });

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var paged = await _svc.GetPagedByEmployeeAsync(employeeId, page, pageSize, ct);
        var dtoItems = _mapper.Map<List<DepartmentAuthorityDto>>(paged.Items);

        return Ok(new
        {
            items = dtoItems,
            page = paged.Page,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount,
            totalPages = paged.TotalPages,
            hasPreviousPage = paged.HasPreviousPage,
            hasNextPage = paged.HasNextPage
        });
    }

    /// <summary>
    /// Consulta la denominación de autoridad activa de un empleado por su cédula de identidad.
    /// Realiza el join: People (IdCard) → Employees (PersonID) → DepartmentAuthorities (EmployeeID).
    /// </summary>
    /// <param name="idCard">Número de cédula de identidad de la persona.</param>
    /// <returns>DTO con la denominación y datos del empleado, o 404 si no se encuentra.</returns>
    [HttpGet("denomination/by-idcard/{idCard}")]
    [ProducesResponseType(typeof(DepartmentAuthorityDenominationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDenominationByIdCard(
        [FromRoute] string idCard,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idCard))
            return BadRequest(new { message = "La cédula de identidad es requerida." });

        var result = await _svc.GetDenominationByIdCardAsync(idCard.Trim(), ct);

        if (result is null)
            return NotFound(new { message = $"No se encontró una autoridad activa para la cédula '{idCard}'." });

        return Ok(result);
    }

    /// <summary>
    /// Obtiene un registro de autoridad de departamento por su ID.
    /// </summary>
    /// <param name="id">Identificador único del registro.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DepartmentAuthorityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var entity = await _svc.GetByIdAsync(id, ct);
        return entity is null
            ? NotFound(new { message = $"No se encontró la autoridad con ID {id}." })
            : Ok(_mapper.Map<DepartmentAuthorityDto>(entity));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST — Creación
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo registro de autoridad de departamento.
    /// </summary>
    /// <param name="dto">Datos del nuevo nombramiento.</param>
    [HttpPost]
    [ProducesResponseType(typeof(DepartmentAuthorityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] DepartmentAuthorityCreateDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var entity = _mapper.Map<DepartmentAuthority>(dto);
            var created = await _svc.CreateAsync(entity, ct);
            var createdDto = _mapper.Map<DepartmentAuthorityDto>(created);
            return CreatedAtAction(nameof(GetById), new { id = createdDto.AuthorityId }, createdDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error al crear la autoridad de departamento.", error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT — Actualización
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Actualiza un registro de autoridad de departamento existente.
    /// </summary>
    /// <param name="id">ID del registro a actualizar.</param>
    /// <param name="dto">Datos actualizados del nombramiento.</param>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] DepartmentAuthorityUpdateDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var entity = _mapper.Map<DepartmentAuthority>(dto);
            await _svc.UpdateAsync(id, entity, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"No se encontró la autoridad con ID {id}." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error al actualizar la autoridad de departamento.", error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATCH — Cambio de estado
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Activa o desactiva un registro de autoridad de departamento.
    /// </summary>
    /// <param name="id">ID del registro.</param>
    /// <param name="isActive">Estado deseado: true = activo, false = inactivo.</param>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetStatus(
        [FromRoute] int id,
        [FromQuery] bool isActive,
        CancellationToken ct)
    {
        try
        {
            var entity = await _svc.GetByIdAsync(id, ct);
            if (entity is null)
                return NotFound(new { message = $"No se encontró la autoridad con ID {id}." });

            entity.IsActive = isActive;
            await _svc.UpdateAsync(id, entity, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error al cambiar el estado de la autoridad.", error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE — Eliminación
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Elimina un registro de autoridad de departamento por su ID.
    /// </summary>
    /// <param name="id">ID del registro a eliminar.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"No se encontró la autoridad con ID {id}." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error al eliminar la autoridad de departamento.", error = ex.Message });
        }
    }
}
