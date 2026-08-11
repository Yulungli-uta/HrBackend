using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.ContractRequest;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

using ContractDocumentStatusDto = WsUtaSystem.Application.DTOs.Contracts.ContractDocumentStatusDto;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractsService _service;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserAccessScopeService _accessScopeService;
    private readonly IRecordAccessGuard _accessGuard;

    public ContractsController(
        IContractsService service,
        IMapper mapper,
        ICurrentUserService currentUser,
        IUserAccessScopeService accessScopeService,
        IRecordAccessGuard accessGuard)
    {
        _service = service;
        _mapper = mapper;
        _currentUser = currentUser;
        _accessScopeService = accessScopeService;
        _accessGuard = accessGuard;
    }

    /// <summary>Carga un contrato y valida que su departamento esté dentro del alcance del usuario. Retorna null si no existe.</summary>
    private async Task<Contracts?> LoadWithAccessCheckAsync(int id, CancellationToken ct)
    {
        var entity = await _service.GetByIdAsync(id, ct);
        if (entity is null) return null;
        await _accessGuard.EnsureDepartmentAsync(entity.DepartmentID, "CONTRACTS", ct);
        return entity;
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });

    /// <summary>Lista todos los contratos.</summary>
    [HttpGet]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ContractsDto>>(await _service.GetAllAsync(ct)));

    /// <summary>Retorna un resultado paginado de contratos.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
    /// <param name="search">Texto libre sobre código o descripción (NO busca cédula/nombre — usar <paramref name="personId"/> para filtrar por empleado).</param>
    /// <param name="statusTypeId">Filtro por TypeId del estado (CONTRACT_STATUS).</param>
    /// <param name="certificationId">Filtro por CertificationID vinculado.</param>
    /// <param name="year">Filtro por año de creación (0 = todos).</param>
    /// <param name="employeeId">Filtro por EmployeeID del titular del contrato (se resuelve a PersonID internamente).</param>
    /// <param name="startDateFrom">Filtro: StartDate mayor o igual a esta fecha.</param>
    /// <param name="startDateTo">Filtro: StartDate menor o igual a esta fecha.</param>
    /// <param name="sortDirection">Dirección del orden sobre CreatedAt: asc | desc (por defecto desc).</param>
    [HttpGet("paged")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? statusTypeId = null,
        [FromQuery] int? certificationId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] string? sortDirection = "desc",
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var term = hasSearch ? search!.Trim().ToLower() : string.Empty;
        var hasYear = year.HasValue && year.Value > 0;
        var ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        // EmployeeCombobox (frontend) solo conoce EmployeeID — Contracts filtra por PersonID.
        int? personId = employeeId.HasValue
            ? await _service.ResolvePersonIdByEmployeeIdAsync(employeeId.Value, ct)
            : null;

        // null = sin restricción de departamento (GLOBAL o sin scopes asignados aún).
        List<int>? allowedDeptIds = _currentUser.EmployeeId.HasValue
            ? await _accessScopeService.GetAllowedDepartmentIdsAsync(_currentUser.EmployeeId.Value, "CONTRACTS", ct)
            : null;
        var hasDeptScope = allowedDeptIds is not null;

        System.Linq.Expressions.Expression<Func<Contracts, bool>>? predicate = null;

        if (hasSearch || statusTypeId.HasValue || certificationId.HasValue || hasYear
            || personId.HasValue || startDateFrom.HasValue || startDateTo.HasValue || hasDeptScope)
        {
            predicate = c =>
                (!hasSearch || c.ContractCode.ToLower().Contains(term) ||
                    (c.ContractDescription != null && c.ContractDescription.ToLower().Contains(term))) &&
                (!statusTypeId.HasValue    || c.Status == statusTypeId.Value) &&
                (!certificationId.HasValue || c.CertificationID == certificationId.Value) &&
                (!hasYear                  || (c.CreatedAt != null && c.CreatedAt.Value.Year == year!.Value)) &&
                (!personId.HasValue        || c.PersonID == personId.Value) &&
                (!startDateFrom.HasValue   || c.StartDate >= startDateFrom.Value) &&
                (!startDateTo.HasValue     || c.StartDate <= startDateTo.Value) &&
                (!hasDeptScope             || allowedDeptIds!.Contains(c.DepartmentID));
        }

        var pagedEntities = predicate is not null
            ? await _service.GetPagedAsync(predicate, page, pageSize, ct, orderBy: c => (object)c.CreatedAt!, ascending: ascending)
            : await _service.GetPagedAsync(page, pageSize, ct, orderBy: c => (object)c.CreatedAt!, ascending: ascending);

        var dtoItems = _mapper.Map<List<ContractsDto>>(pagedEntities.Items);

        return Ok(new
        {
            items = dtoItems,
            page = pagedEntities.Page,
            pageSize = pagedEntities.PageSize,
            totalCount = pagedEntities.TotalCount,
            totalPages = pagedEntities.TotalPages,
            hasPreviousPage = pagedEntities.HasPreviousPage,
            hasNextPage = pagedEntities.HasNextPage
        });
    }

    /// <summary>
    /// Retorna los contratos creados por el usuario autenticado (paginado).
    /// El filtro por empleado se aplica en el servidor usando el token JWT — el cliente
    /// nunca envía el employeeId en la URL.
    /// </summary>
    [HttpGet("my/paged")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetMyPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? statusTypeId = null,
        [FromQuery] int? year = null,
        [FromQuery] string? sortDirection = "desc",
        CancellationToken ct = default)
    {
        var employeeId = _currentUser.EmployeeId;
        if (employeeId is null) return Unauthorized();

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var term = hasSearch ? search!.Trim().ToLower() : string.Empty;
        var hasYear = year.HasValue && year.Value > 0;
        var ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        System.Linq.Expressions.Expression<Func<Contracts, bool>> predicate = c =>
            c.CreatedBy == employeeId.Value &&
            (!hasSearch || c.ContractCode.ToLower().Contains(term) ||
                (c.ContractDescription != null && c.ContractDescription.ToLower().Contains(term))) &&
            (!statusTypeId.HasValue || c.Status == statusTypeId.Value) &&
            (!hasYear || (c.CreatedAt != null && c.CreatedAt.Value.Year == year!.Value));

        var pagedEntities = await _service.GetPagedAsync(
            predicate, page, pageSize, ct,
            orderBy: c => (object)c.CreatedAt!, ascending: ascending);

        var dtoItems = _mapper.Map<List<ContractsDto>>(pagedEntities.Items);

        return Ok(new
        {
            items = dtoItems,
            page = pagedEntities.Page,
            pageSize = pagedEntities.PageSize,
            totalCount = pagedEntities.TotalCount,
            totalPages = pagedEntities.TotalPages,
            hasPreviousPage = pagedEntities.HasPreviousPage,
            hasNextPage = pagedEntities.HasNextPage
        });
    }

    /// <summary>Obtiene un contrato por ID.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        Contracts? entity;
        try { entity = await LoadWithAccessCheckAsync(id, ct); }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        if (entity is null) return NotFound();
        return Ok(_mapper.Map<ContractsDto>(entity));
    }

    /// <summary>Crea un nuevo contrato.</summary>
    //[HttpPost]
    //public async Task<IActionResult> Create(ContractsCreateDto dto, CancellationToken ct)
    //{
    //    var entity = _mapper.Map<Contracts>(dto);
    //    var created = await _service.CreateAsync(entity, ct);
    //    return CreatedAtAction(
    //        nameof(GetById),
    //        new { id = created.ContractID },
    //        _mapper.Map<ContractsDto>(created)
    //    );
    //}

    /// <summary>Crea un nuevo contrato. Para contratos raíz valida certificación aprobada y cupo disponible.</summary>
    [HttpPost]
    [RequirePermission("CONTRACTS.CREATE")]
    [ProducesResponseType(typeof(CreateContractResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(ContractsCreateDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<Contracts>(dto);

        if (_currentUser.EmployeeId.HasValue)
        {
            try
            {
                await _accessScopeService.EnsureDepartmentAllowedAsync(
                    _currentUser.EmployeeId.Value, "CONTRACTS", entity.DepartmentID, ct);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        var created = await _service.CreateAndNotifyAsync(entity, ct, dto.IsHistoricalEntry);

        GenerateContractDocumentResponse? document = null;

        if (dto.GenerateDocument)
        {
            try
            {
                var generatedBy = _currentUser.EmployeeId ?? 0;

                document = await _service.GenerateDocumentAsync(
                    created.ContractID,
                    new GenerateContractDocumentRequest(dto.DocumentOverrides, false),
                    generatedBy,
                    ct);

                created = await _service.GetByIdAsync(created.ContractID, ct) ?? created;
            }
            catch
            {
                await _service.DeleteAsync(created.ContractID, ct);
                throw;
            }
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.ContractID },
            new CreateContractResponse(
                _mapper.Map<ContractsDto>(created),
                document
            )
        );
    }

    /// <summary>Actualiza un contrato existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CONTRACTS.UPDATE")]
    public async Task<IActionResult> Update(int id, ContractsUpdateDto dto, CancellationToken ct)
    {
        if (dto.ContractID != 0 && dto.ContractID != id)
            return BadRequest("ContractID no coincide con la ruta.");

        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        try
        {
            await _service.UpdateAsync(id, dto, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return NoContent();
    }

    /// <summary>
    /// Corrige los datos de un contrato ya existente, en CUALQUIER estado (incluido VIGENTE).
    /// A diferencia de <see cref="Update"/> (solo BORRADOR/GENERADO), exige un motivo
    /// obligatorio y queda registrada en el historial de auditoría (HR.Audit,
    /// Action=CORRECTION). Requiere el permiso elevado CONTRACTS.MANAGE.
    /// </summary>
    [HttpPut("{id:int}/correct")]
    [RequirePermission("CONTRACTS.MANAGE")]
    public async Task<IActionResult> Correct(int id, [FromBody] CorrectContractRequest request, CancellationToken ct)
    {
        if (request.Data.ContractID != 0 && request.Data.ContractID != id)
            return BadRequest("ContractID no coincide con la ruta.");

        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        try
        {
            await _service.CorrectAsync(id, request.Data, request.Reason, ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
        return NoContent();
    }

    /// <summary>Elimina un contrato por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("CONTRACTS.DELETE")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Obtiene los estados permitidos para la siguiente transición.</summary>
    [HttpGet("status/allowed")]
    public async Task<IActionResult> Allowed([FromQuery] int currentStatusTypeId, CancellationToken ct)
    {
        var next = await _service.GetAllowedNextStatusesAsync(currentStatusTypeId, ct);
        return Ok(next);
    }

    /// <summary>Cambia el estado de un contrato.</summary>
    [HttpPost("{id:int}/status")]
    [RequirePermission("CONTRACTS.UPDATE")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ContractChangeStatusDto dto, CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        await _service.ChangeStatusAsync(id, dto.ToStatusTypeID, dto.Comment, ct);
        return NoContent();
    }

    /// <summary>Obtiene el historial de estados de un contrato.</summary>
    [HttpGet("{id:int}/history")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> History(int id, CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var items = await _service.GetStatusHistoryAsync(id, ct);
        return Ok(items);
    }

    /// <summary>Obtiene los addendums de un contrato.</summary>
    [HttpGet("{id:int}/addendums")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> Addendums(int id, CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var items = await _service.GetAddendumsAsync(id, ct);
        return Ok(items);
    }

    // ── Motor documental ─────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene el estado del documento institucional vinculado a un contrato.
    /// Devuelve null si el contrato no tiene documento generado aún.
    /// </summary>
    [HttpGet("{id:int}/document-status")]
    [RequirePermission("CONTRACTS.READ")]
    [ProducesResponseType(typeof(ContractDocumentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DocumentStatus([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var result = await _service.GetDocumentStatusAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Vincula un documento generado al contrato y lo congela.
    /// Un documento congelado no se regenera automáticamente aunque cambien los datos del contrato.
    /// </summary>
    [HttpPatch("{id:int}/freeze-document")]
    [RequirePermission("CONTRACTS.GENERATE_DOCUMENT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FreezeDocument(
        [FromRoute] int id,
        [FromBody] FreezeDocumentRequest request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        await _service.FreezeDocumentAsync(id, request.DocumentId, request.TemplateVersion, ct);
        return NoContent();
    }

    /// <summary>
    /// Descongela el documento de un contrato para permitir regenerar el PDF.
    /// </summary>
    [HttpPatch("{id:int}/unfreeze-document")]
    [RequirePermission("CONTRACTS.GENERATE_DOCUMENT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfreezeDocument([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        await _service.UnfreezeDocumentAsync(id, ct);
        return NoContent();
    }
    [HttpPost("{id:int}/generate-document")]
    [RequirePermission("CONTRACTS.GENERATE_DOCUMENT")]
    [ProducesResponseType(typeof(GenerateContractDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateDocument(
    [FromRoute] int id,
    [FromBody] GenerateContractDocumentRequest? request,
    CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var generatedBy = _currentUser.EmployeeId ?? 0;

        var result = await _service.GenerateDocumentAsync(
            id,
            request ?? new GenerateContractDocumentRequest(),
            generatedBy,
            ct);

        return Ok(result);
    }

    [HttpPost("{id:int}/document/pending-signatures")]
    [RequirePermission("CONTRACTS.GENERATE_DOCUMENT")]
    public async Task<IActionResult> MarkDocumentPendingSignatures(
        [FromRoute] int id,
        [FromBody] ContractDocumentCommentRequest? request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;

        await _service.MarkDocumentPendingSignaturesAsync(
            id,
            request?.Comment,
            updatedBy,
            ct);

        return NoContent();
    }

    [HttpPost("{id:int}/document/upload-signed")]
    [RequirePermission("CONTRACTS.GENERATE_DOCUMENT")]
    public async Task<IActionResult> UploadSignedDocument(
        [FromRoute] int id,
        [FromBody] UploadSignedContractDocumentRequest request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;

        await _service.UploadSignedDocumentAsync(
            id,
            request,
            updatedBy,
            ct);

        return NoContent();
    }

    [HttpPost("{id:int}/document/finalize")]
    [RequirePermission("CONTRACTS.GENERATE_DOCUMENT")]
    public async Task<IActionResult> FinalizeDocument(
        [FromRoute] int id,
        [FromBody] ContractDocumentCommentRequest? request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;

        await _service.FinalizeDocumentAsync(
            id,
            request?.Comment,
            updatedBy,
            ct);

        return NoContent();
    }

    [HttpPost("{id:int}/document/cancel")]
    [RequirePermission("CONTRACTS.GENERATE_DOCUMENT")]
    public async Task<IActionResult> CancelDocument(
        [FromRoute] int id,
        [FromBody] CancelContractDocumentRequest request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;

        await _service.CancelDocumentAsync(
            id,
            request,
            updatedBy,
            ct);

        return NoContent();
    }
}
