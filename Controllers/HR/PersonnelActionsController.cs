// ============================================================
// WsUtaSystem.Controllers.Documents.PersonnelActionsController
// Motor Documental Institucional — Acciones de Personal LOSEP/RLOSEP
// ============================================================
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.PersonnelActions;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Documents;

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
    private readonly ILogger<PersonnelActionsController> _logger;

    public PersonnelActionsController(
        IPersonnelActionService personnelActionService,
        ICurrentUserService currentUser,
        IUserAccessScopeService accessScopeService,
        ILogger<PersonnelActionsController> logger)
    {
        _personnelActionService = personnelActionService;
        _currentUser            = currentUser;
        _accessScopeService     = accessScopeService;
        _logger                 = logger;
    }

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
    [ProducesResponseType(typeof(PersonnelActionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var action = await _personnelActionService.GetDetailByIdAsync(id, ct);
        return action is null ? NotFound() : Ok(action);
    }

    /// <summary>
    /// Obtiene todas las acciones de personal de un empleado específico.
    /// Útil para el historial de acciones del empleado.
    /// </summary>
    /// <param name="employeeId">Identificador del empleado.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("by-employee/{employeeId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<PersonnelActionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEmployee([FromRoute] int employeeId, CancellationToken ct)
    {
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdatePersonnelActionRequest request,
        CancellationToken ct)
    {
        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _personnelActionService.UpdateAsync(id, request, updatedBy, ct);
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
    [ProducesResponseType(typeof(CreatePersonnelActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        [FromRoute] int id,
        [FromBody] ApprovePersonnelActionRequest request,
        CancellationToken ct)
    {
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
    [ProducesResponseType(typeof(CreatePersonnelActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateDocument(
        [FromRoute] int id,
        [FromBody] GenerateDocumentOverridesRequest? request,
        CancellationToken ct)
    {
        var generatedBy = _currentUser.EmployeeId ?? 0;
        var result = await _personnelActionService.GenerateDocumentAsync(
            id, request?.Overrides, generatedBy, ct);

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewDocument(
        [FromBody] PreviewPersonnelActionRequest request,
        CancellationToken ct)
    {
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkPendingSignatures(
        [FromRoute] int id,
        [FromBody] CommentRequest? request,
        CancellationToken ct)
    {
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadSignedDocument(
        [FromRoute] int id,
        [FromBody] UploadSignedDocumentRequest request,
        CancellationToken ct)
    {
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Finalize(
        [FromRoute] int id,
        [FromBody] CommentRequest? request,
        CancellationToken ct)
    {
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        [FromRoute] int id,
        [FromBody] CancelPersonnelActionRequest request,
        CancellationToken ct)
    {
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
    [ProducesResponseType(typeof(IReadOnlyList<PersonnelActionStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory([FromRoute] int id, CancellationToken ct)
    {
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
