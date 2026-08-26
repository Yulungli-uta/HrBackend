// ============================================================
// WsUtaSystem.Controllers.Documents.PersonnelActionsController
// Motor Documental Institucional — Acciones de Personal LOSEP/RLOSEP
// ============================================================
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.PersonnelActions;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Expone los endpoints REST para la gestión del ciclo de vida completo
/// de las acciones de personal (nombramientos, licencias, comisiones, etc.)
/// según la normativa LOSEP/RLOSEP del Ecuador.
///
/// Integra la creación de la acción con la generación automática del documento PDF
/// institucional cuando <c>GenerateDocument = true</c>.
///
/// Rutas base: <c>api/v1/documents/personnel-actions</c>
/// </summary>
[ApiController]
[Route("personnel-actions")]
public sealed class PersonnelActionsController : ControllerBase
{
    private readonly IPersonnelActionService _personnelActionService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserAccessScopeService _accessScopeService;
    private readonly IRecordAccessGuard _accessGuard;
    private readonly IUserActionPermissionService _permissionService;
    private readonly ILogger<PersonnelActionsController> _logger;

    public PersonnelActionsController(
        IPersonnelActionService personnelActionService,
        ICurrentUserService currentUser,
        IUserAccessScopeService accessScopeService,
        IRecordAccessGuard accessGuard,
        IUserActionPermissionService permissionService,
        ILogger<PersonnelActionsController> logger)
    {
        _personnelActionService = personnelActionService;
        _currentUser            = currentUser;
        _accessScopeService     = accessScopeService;
        _accessGuard            = accessGuard;
        _permissionService      = permissionService;
        _logger                 = logger;
    }

    /// <summary>
    /// Usuarios con el permiso elevado de Corrección (PERSONNEL_ACTIONS.MANAGE) tienen alcance
    /// sobre cualquier departamento — verificado en servidor contra los roles reales del token,
    /// nunca un flag del cliente.
    /// </summary>
    private async Task<bool> HasManagePermissionAsync(CancellationToken ct)
    {
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        return await _permissionService.HasPermissionAsync(roles, "PERSONNEL_ACTIONS.MANAGE", ct);
    }

    /// <summary>Carga el detalle de una acción y valida que su empleado esté dentro del alcance del usuario. Retorna null si no existe.</summary>
    private async Task<PersonnelActionDetailDto?> LoadWithAccessCheckAsync(int id, CancellationToken ct)
    {
        var action = await _personnelActionService.GetDetailByIdAsync(id, ct);
        if (action is null) return null;

        if (await HasManagePermissionAsync(ct)) return action;

        if (action.EmployeeId > 0)
        {
            await _accessGuard.EnsureEmployeeRecordAsync(action.EmployeeId, "PERSONNEL_ACTIONS", ct);
        }
        else
        {
            // Acción de ingreso (ej. Nombramiento) para una persona que aún no tiene registro
            // de Empleado: EmployeeID es NULL a propósito (la acción solo referencia PersonId).
            // No se puede resolver el departamento vía empleado, así que se usa el departamento
            // propio de la acción (mismo criterio que Create(), línea ~156).
            var departmentId = action.DestinationDepartmentId ?? action.OriginDepartmentId
                ?? throw new UnauthorizedAccessException("No se pudo determinar el departamento del registro solicitado.");
            await _accessGuard.EnsureDepartmentAsync(departmentId, "PERSONNEL_ACTIONS", ct);
        }

        return action;
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });

    // ──────────────────────────────────────────────────────────────────────────
    // CONSULTAS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene una lista paginada de acciones de personal con filtros opcionales.
    /// Permite filtrar por empleado, tipo de acción, estado y rango de fechas.
    /// </summary>
    /// <param name="filter">Parámetros de búsqueda y paginación.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet]
    [RequirePermission("PERSONNEL_ACTIONS.READ")]
    [ProducesResponseType(typeof(PagedPersonnelActionResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] PersonnelActionQueryFilter filter,
        CancellationToken ct)
    {
        // null = sin restricción de departamento (GLOBAL o sin scopes asignados aún).
        var allowedDeptIds = _currentUser.EmployeeId.HasValue
            ? await _accessScopeService.GetAllowedDepartmentIdsAsync(_currentUser.EmployeeId.Value, "PERSONNEL_ACTIONS", ct)
            : null;

        var effectiveFilter = allowedDeptIds is null ? filter : filter with { AllowedDepartmentIds = allowedDeptIds };

        var result = await _personnelActionService.GetPagedAsync(effectiveFilter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle completo de una acción de personal,
    /// incluyendo los datos del empleado, contrato y documento PDF generado.
    /// </summary>
    /// <param name="id">Identificador de la acción de personal.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("{id:int}")]
    [RequirePermission("PERSONNEL_ACTIONS.READ")]
    [ProducesResponseType(typeof(PersonnelActionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        PersonnelActionDetailDto? action;
        try { action = await LoadWithAccessCheckAsync(id, ct); }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        return action is null ? NotFound() : Ok(action);
    }

    /// <summary>
    /// Obtiene todas las acciones de personal de un empleado específico.
    /// Útil para el historial de acciones del empleado.
    /// </summary>
    /// <param name="employeeId">Identificador del empleado.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("by-employee/{employeeId:int}")]
    [RequirePermission("PERSONNEL_ACTIONS.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<PersonnelActionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEmployee([FromRoute] int employeeId, CancellationToken ct)
    {
        if (_currentUser.EmployeeId != employeeId)
        {
            try { await _accessGuard.EnsureEmployeeRecordAsync(employeeId, "PERSONNEL_ACTIONS", ct); }
            catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }
        }

        var actions = await _personnelActionService.GetByEmployeeIdAsync(employeeId, ct);
        return Ok(actions);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CREACIÓN Y MODIFICACIÓN
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una nueva acción de personal.
    /// Si <c>GenerateDocument = true</c>, genera automáticamente el PDF institucional
    /// usando la plantilla configurada para el tipo de acción.
    /// </summary>
    /// <param name="request">Datos de la acción de personal.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost]
    [RequirePermission("PERSONNEL_ACTIONS.CREATE")]
    [ProducesResponseType(typeof(CreatePersonnelActionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePersonnelActionRequest request,
        CancellationToken ct)
    {
        var createdBy = _currentUser.EmployeeId ?? 0;

        // Control de departamento se aplica solo al destino (a dónde va la persona).
        // El origen puede ser de cualquier departamento. Si no hay destino, se valida
        // contra el origen (única referencia de departamento disponible para la acción).
        var departmentToValidate = request.DestinationDepartmentId ?? request.OriginDepartmentId;
        if (_currentUser.EmployeeId.HasValue && departmentToValidate.HasValue)
        {
            try
            {
                await _accessScopeService.EnsureDepartmentAllowedAsync(
                    _currentUser.EmployeeId.Value, "PERSONNEL_ACTIONS", departmentToValidate.Value, ct);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        var result = await _personnelActionService.CreateAsync(request, createdBy, ct);

        _logger.LogInformation(
            "Acción de personal creada. ActionId={ActionId} EmployeeId={EmpId} Tipo={ActionType} GeneraDoc={GenDoc} por UserId={UserId}",
            result.ActionId, request.EmployeeId, request.ActionTypeId, request.GenerateDocument, createdBy);

        return CreatedAtAction(nameof(GetById), new { id = result.ActionId }, result);
    }

    /// <summary>
    /// Actualiza los datos de una acción de personal en estado <c>Draft</c>.
    /// No se permite modificar acciones ya aprobadas o ejecutadas.
    /// </summary>
    /// <param name="id">Identificador de la acción de personal.</param>
    /// <param name="request">Datos actualizados.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPut("{id:int}")]
    [RequirePermission("PERSONNEL_ACTIONS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdatePersonnelActionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _personnelActionService.UpdateAsync(id, request, updatedBy, ct);
        return NoContent();
    }

    /// <summary>
    /// Corrige los datos de una acción de personal ya existente, en CUALQUIER estado
    /// (incluido VIGENTE/FINALIZADO). A diferencia de <see cref="Update"/> (solo
    /// BORRADOR/GENERADO), exige un motivo obligatorio y queda registrada en el historial
    /// de auditoría (HR.Audit, Action=CORRECTION) con el detalle de los campos modificados.
    /// Requiere el permiso elevado PERSONNEL_ACTIONS.MANAGE.
    /// </summary>
    /// <param name="id">Identificador de la acción de personal.</param>
    /// <param name="request">Motivo de la corrección y datos actualizados.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPut("{id:int}/correct")]
    [RequirePermission("PERSONNEL_ACTIONS.MANAGE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Correct(
        [FromRoute] int id,
        [FromBody] CorrectPersonnelActionRequest request,
        CancellationToken ct)
    {
        // Sin guard de departamento a propósito: la Corrección es una capacidad elevada
        // (permiso PERSONNEL_ACTIONS.MANAGE) pensada para arreglar registros de cualquier
        // departamento, no solo el del usuario que corrige.
        if (await _personnelActionService.GetDetailByIdAsync(id, ct) is null) return NotFound();

        var correctedBy = _currentUser.EmployeeId ?? 0;

        try
        {
            await _personnelActionService.CorrectAsync(id, request.Data, request.Reason, correctedBy, ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        _logger.LogInformation(
            "Acción de personal Id={ActionId} CORREGIDA por EmployeeId={UserId}. Motivo: {Reason}",
            id, correctedBy, request.Reason);

        return NoContent();
    }

    /// <summary>
    /// Corrige directamente el estado de una acción de personal (sin pasar por el flujo normal
    /// de transición ni disparar sus efectos secundarios). Exige motivo obligatorio y queda
    /// registrada en el historial de estados. Requiere el permiso elevado PERSONNEL_ACTIONS.MANAGE.
    /// </summary>
    [HttpPut("{id:int}/correct-status")]
    [RequirePermission("PERSONNEL_ACTIONS.MANAGE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CorrectStatus(
        [FromRoute] int id,
        [FromBody] CorrectPersonnelActionStatusRequest request,
        CancellationToken ct)
    {
        // Sin guard de departamento a propósito: ver comentario en Correct().
        if (await _personnelActionService.GetDetailByIdAsync(id, ct) is null) return NotFound();

        var correctedBy = _currentUser.EmployeeId ?? 0;

        try
        {
            await _personnelActionService.CorrectStatusAsync(id, request.NewStatus, request.Reason, correctedBy, ct);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            return BadRequest(new { message = ex.Message });
        }

        _logger.LogInformation(
            "Acción de personal Id={ActionId} ESTADO CORREGIDO a '{NewStatus}' por EmployeeId={UserId}. Motivo: {Reason}",
            id, request.NewStatus, correctedBy, request.Reason);

        return NoContent();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FLUJO DE APROBACIÓN
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Aprueba y ejecuta una acción de personal.
    /// Cambia el estado a <c>Approved</c> y puede regenerar el documento PDF
    /// con los datos definitivos de aprobación.
    /// </summary>
    /// <param name="id">Identificador de la acción de personal.</param>
    /// <param name="request">Datos de aprobación (observaciones, fecha efectiva).</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("{id:int}/approve")]
    [RequirePermission("PERSONNEL_ACTIONS.APPROVE")]
    [ProducesResponseType(typeof(CreatePersonnelActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        [FromRoute] int id,
        [FromBody] ApprovePersonnelActionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var approvedBy = _currentUser.EmployeeId ?? 0;
        var result = await _personnelActionService.ApproveAsync(id, request, approvedBy, ct);

        _logger.LogInformation(
            "Acción de personal Id={ActionId} aprobada por EmployeeId={UserId}",
            id, approvedBy);

        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GENERACIÓN DE DOCUMENTO
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Genera o regenera el documento PDF asociado a una acción de personal.
    /// Permite sobreescribir valores de campos específicos mediante el diccionario
    /// <c>overrides</c> (clave = nombre del campo, valor = texto a usar).
    /// </summary>
    /// <param name="id">Identificador de la acción de personal.</param>
    /// <param name="request">Overrides opcionales de campos del documento.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("{id:int}/generate-document")]
    [RequirePermission("PERSONNEL_ACTIONS.GENERATE_DOCUMENT")]
    [ProducesResponseType(typeof(CreatePersonnelActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateDocument(
        [FromRoute] int id,
        [FromBody] GenerateDocumentOverridesRequest? request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var generatedBy = _currentUser.EmployeeId ?? 0;
        CreatePersonnelActionResponse result;
        try
        {
            result = await _personnelActionService.GenerateDocumentAsync(
                id, request?.Overrides, generatedBy, ct);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        _logger.LogInformation(
            "Documento PDF generado/regenerado para Acción Id={ActionId} por EmployeeId={UserId}",
            id, generatedBy);

        return Ok(result);
    }

    /// <summary>
    /// Genera un PDF de previsualización sin guardar ningún registro en la BD.
    /// Permite al usuario ver el aspecto del documento antes de crear la acción.
    /// </summary>
    /// <param name="request">EmployeeId y overrides del formulario.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("preview-document")]
    [RequirePermission("PERSONNEL_ACTIONS.GENERATE_DOCUMENT")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewDocument(
        [FromBody] PreviewPersonnelActionRequest request,
        CancellationToken ct)
    {
        try
        {
            await _accessGuard.EnsureEmployeeRecordAsync(request.EmployeeId, "PERSONNEL_ACTIONS", ct);
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        _logger.LogInformation(
            "Preview Acción Personal request. EmployeeId={EmployeeId}, Overrides={Overrides}",
            request.EmployeeId,
            request.Overrides == null
                ? "NULL"
                : string.Join("; ", request.Overrides.Select(x => $"{x.Key}={x.Value}")));

        var (pdfBase64, fileName) = await _personnelActionService.PreviewDocumentAsync(
            request.EmployeeId,
            request.Overrides ?? new Dictionary<string, string>(),
            ct);

        _logger.LogInformation(
            "Previsualización de documento generada para EmployeeId={EmpId}.", request.EmployeeId);

        return Ok(new { pdfBase64, fileName });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FLUJO DE ESTADOS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Transición GENERADO → PENDIENTE_FIRMAS.
    /// Indica que el documento fue impreso y está esperando firma física.
    /// </summary>
    [HttpPost("{id:int}/mark-pending-signatures")]
    [RequirePermission("PERSONNEL_ACTIONS.GENERATE_DOCUMENT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkPendingSignatures(
        [FromRoute] int id,
        [FromBody] CommentRequest? request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _personnelActionService.MarkPendingSignaturesAsync(id, request?.Comment, updatedBy, ct);

        _logger.LogInformation(
            "Acción Id={ActionId} marcada PENDIENTE_FIRMAS por EmployeeId={UserId}", id, updatedBy);

        return NoContent();
    }

    /// <summary>
    /// Transición PENDIENTE_FIRMAS → FIRMADO_CARGADO.
    /// Adjunta el archivo del documento firmado escaneado.
    /// </summary>
    [HttpPost("{id:int}/upload-signed-document")]
    [RequirePermission("PERSONNEL_ACTIONS.GENERATE_DOCUMENT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadSignedDocument(
        [FromRoute] int id,
        [FromBody] UploadSignedDocumentRequest request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _personnelActionService.UploadSignedDocumentAsync(id, request, updatedBy, ct);

        _logger.LogInformation(
            "Documento firmado cargado para Acción Id={ActionId} StoredFileId={FileId} por EmployeeId={UserId}",
            id, request.StoredFileId, updatedBy);

        return NoContent();
    }

    /// <summary>
    /// Transición FIRMADO_CARGADO → FINALIZADO.
    /// Cierra el ciclo de vida de la acción de personal.
    /// </summary>
    [HttpPost("{id:int}/finalize")]
    [RequirePermission("PERSONNEL_ACTIONS.GENERATE_DOCUMENT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Finalize(
        [FromRoute] int id,
        [FromBody] CommentRequest? request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _personnelActionService.FinalizeAsync(id, request?.Comment, updatedBy, ct);

        _logger.LogInformation(
            "Acción Id={ActionId} FINALIZADA por EmployeeId={UserId}", id, updatedBy);

        return NoContent();
    }

    /// <summary>
    /// Anula una acción de personal. Requiere motivo obligatorio.
    /// No aplicable si ya está FINALIZADA.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("PERSONNEL_ACTIONS.CANCEL")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        [FromRoute] int id,
        [FromBody] CancelPersonnelActionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _personnelActionService.CancelAsync(id, request, updatedBy, ct);

        _logger.LogInformation(
            "Acción Id={ActionId} ANULADA por EmployeeId={UserId}. Razón: {Reason}",
            id, updatedBy, request.Reason);

        return NoContent();
    }

    /// <summary>
    /// Obtiene el historial completo de cambios de estado de una acción de personal.
    /// </summary>
    [HttpGet("{id:int}/history")]
    [RequirePermission("PERSONNEL_ACTIONS.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<PersonnelActionStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            if (await LoadWithAccessCheckAsync(id, ct) is null) return NotFound();
        }
        catch (UnauthorizedAccessException ex) { return Forbid403(ex.Message); }

        var history = await _personnelActionService.GetStatusHistoryAsync(id, ct);
        return Ok(history);
    }
}

/// <summary>Request para sobreescrituras de campos al generar el documento.</summary>
public sealed record GenerateDocumentOverridesRequest(
    Dictionary<string, string>? Overrides
);

/// <summary>Request genérico que acepta un comentario opcional.</summary>
public sealed record CommentRequest(string? Comment);
