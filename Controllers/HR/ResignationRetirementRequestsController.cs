using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.ResignationRetirement;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Solicitudes de renuncia y jubilación.
/// El solicitante siempre es el usuario autenticado — el backend resuelve el EmployeeId
/// desde <see cref="ICurrentUserService"/> y jamás confía en un EmployeeId enviado por el frontend.
///
/// Rutas "mis solicitudes" (<c>/my/...</c>): solo ven y afectan la solicitud del propio usuario.
/// Rutas de Recursos Humanos (raíz del controller): listado/revisión de todas las solicitudes,
/// filtradas por el scope de departamento del revisor (<see cref="IUserAccessScopeService"/>).
/// </summary>
[ApiController]
[Route("resignation-retirement-requests")]
public sealed class ResignationRetirementRequestsController : ControllerBase
{
    private const string ModuleCode = "RESIGNATION_RETIREMENT_REQUESTS";

    private readonly IResignationRetirementService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserAccessScopeService _accessScopeService;
    private readonly ILogger<ResignationRetirementRequestsController> _logger;

    public ResignationRetirementRequestsController(
        IResignationRetirementService service,
        ICurrentUserService currentUser,
        IUserAccessScopeService accessScopeService,
        ILogger<ResignationRetirementRequestsController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _accessScopeService = accessScopeService;
        _logger = logger;
    }

    private int RequireEmployeeId()
        => _currentUser.EmployeeId
           ?? throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

    /// <summary>
    /// Valida que el revisor autenticado tenga en su scope (<see cref="IUserAccessScopeService"/>)
    /// el departamento del empleado dueño de la solicitud. Se aplica en TODOS los endpoints de
    /// RRHH que exponen una solicitud puntual (no solo en el listado), para que un empleado sin
    /// scope asignado no pueda ver ni actuar sobre una solicitud ajena solo por conocer su ID.
    /// </summary>
    private async Task EnsureReviewerCanAccessAsync(ResignationRetirementDetailDto detail, CancellationToken ct)
    {
        var reviewerId = RequireEmployeeId();
        var departmentId = detail.Employee.DepartmentId;
        if (departmentId is null) return; // sin departamento no hay contra qué validar

        await _accessScopeService.EnsureDepartmentAllowedAsync(reviewerId, ModuleCode, departmentId.Value, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MIS SOLICITUDES (solicitante)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Información consolidada del empleado autenticado, para la pantalla de creación.</summary>
    [HttpGet("current-employee-info")]
    [ProducesResponseType(typeof(EmployeeConsolidatedInfoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentEmployeeInfo(CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var info = await _service.GetCurrentEmployeeInfoAsync(employeeId, ct);
        return Ok(info);
    }

    /// <summary>
    /// Crea una solicitud de renuncia o jubilación para el usuario autenticado.
    /// No existe forma de indicar otro EmployeeId — se resuelve siempre del token.
    /// </summary>
    [HttpPost("my")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMy([FromBody] CreateResignationRetirementRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _service.CreateAsync(employeeId, request, ct);

        _logger.LogInformation(
            "Solicitud de {RequestType} creada (RequestId={RequestId}) por EmployeeId={EmployeeId}",
            request.RequestType, result.RequestId, employeeId);

        return CreatedAtAction(nameof(GetMyById), new { id = result.RequestId }, result);
    }

    /// <summary>Lista las solicitudes del usuario autenticado. Filtra siempre por su propio EmployeeId.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResignationRetirementResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy([FromQuery] ResignationRetirementQueryFilter filter, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _service.GetMyRequestsAsync(employeeId, filter, ct);
        return Ok(result);
    }

    /// <summary>Detalle de una solicitud propia.</summary>
    [HttpGet("my/{id:int}")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyById([FromRoute] int id, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _service.GetMyRequestDetailAsync(id, employeeId, ct);
        return Ok(result);
    }

    /// <summary>Actualiza una solicitud propia editable (PENDIENTE o DEVUELTO).</summary>
    [HttpPut("my/{id:int}")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMy([FromRoute] int id, [FromBody] UpdateResignationRetirementRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _service.UpdateAsync(id, employeeId, request, ct);
        return Ok(result);
    }

    /// <summary>Cancela (desiste) una solicitud propia mientras no haya sido resuelta por RRHH.</summary>
    [HttpPost("my/{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelMy([FromRoute] int id, [FromBody] CancelResignationRetirementRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        await _service.CancelOwnAsync(id, employeeId, request, ct);

        _logger.LogInformation("Solicitud Id={RequestId} anulada por su propio solicitante EmployeeId={EmployeeId}", id, employeeId);

        return NoContent();
    }

    /// <summary>
    /// Genera (o regenera) la carta de renuncia/jubilación en PDF para descargar, imprimir y firmar.
    /// Solo mientras la solicitud sigue editable (PENDIENTE o DEVUELTO).
    /// </summary>
    [HttpPost("my/{id:int}/generate-document")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateMyDocument([FromRoute] int id, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _service.GenerateDocumentAsync(id, employeeId, ct);
        return Ok(result);
    }

    /// <summary>Descarga la carta generada (sin firmar) — solo el dueño de la solicitud.</summary>
    [HttpGet("my/{id:int}/download-document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadMyDocument([FromRoute] int id, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var (bytes, fileName, contentType) = await _service.DownloadMyDocumentAsync(id, employeeId, ct);
        return File(bytes, contentType, fileName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RECURSOS HUMANOS (revisión)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Lista todas las solicitudes con filtros, restringidas al scope de departamento del revisor.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResignationRetirementResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] ResignationRetirementQueryFilter filter, CancellationToken ct)
    {
        var allowedDeptIds = _currentUser.EmployeeId.HasValue
            ? await _accessScopeService.GetAllowedDepartmentIdsAsync(_currentUser.EmployeeId.Value, ModuleCode, ct)
            : null;

        var effectiveFilter = allowedDeptIds is null ? filter : filter with { AllowedDepartmentIds = allowedDeptIds };
        var result = await _service.GetPagedAsync(effectiveFilter, ct);
        return Ok(result);
    }

    /// <summary>Detalle completo de una solicitud para revisión de Recursos Humanos.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _service.GetDetailByIdAsync(id, ct);
        await EnsureReviewerCanAccessAsync(result, ct);
        return Ok(result);
    }

    /// <summary>Aprueba la solicitud. Requiere contrato/nombramiento/acción vigente del empleado.</summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve([FromRoute] int id, [FromBody] ReviewResignationRetirementRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var reviewedBy = RequireEmployeeId();
        var result = await _service.ApproveAsync(id, reviewedBy, request, ct);

        _logger.LogInformation("Solicitud Id={RequestId} APROBADA por EmployeeId={EmployeeId}", id, reviewedBy);

        return Ok(result);
    }

    /// <summary>Rechaza la solicitud. Observación obligatoria.</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject([FromRoute] int id, [FromBody] ReviewResignationRetirementRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var reviewedBy = RequireEmployeeId();
        var result = await _service.RejectAsync(id, reviewedBy, request, ct);

        _logger.LogInformation("Solicitud Id={RequestId} RECHAZADA por EmployeeId={EmployeeId}", id, reviewedBy);

        return Ok(result);
    }

    /// <summary>Devuelve la solicitud para corrección del solicitante. Observación obligatoria.</summary>
    [HttpPost("{id:int}/return")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Return([FromRoute] int id, [FromBody] ReviewResignationRetirementRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var reviewedBy = RequireEmployeeId();
        var result = await _service.ReturnAsync(id, reviewedBy, request, ct);

        _logger.LogInformation("Solicitud Id={RequestId} DEVUELTA por EmployeeId={EmployeeId}", id, reviewedBy);

        return Ok(result);
    }

    /// <summary>Cancela la solicitud por decisión administrativa de Recursos Humanos.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ResignationRetirementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel([FromRoute] int id, [FromBody] CancelResignationRetirementRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var cancelledBy = RequireEmployeeId();
        var result = await _service.HrCancelAsync(id, cancelledBy, request, ct);

        _logger.LogInformation("Solicitud Id={RequestId} ANULADA por RRHH EmployeeId={EmployeeId}", id, cancelledBy);

        return Ok(result);
    }

    /// <summary>Descarga la carta generada (sin firmar) — vista de Recursos Humanos.</summary>
    [HttpGet("{id:int}/download-document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadDocument([FromRoute] int id, CancellationToken ct)
    {
        var detail = await _service.GetDetailByIdAsync(id, ct);
        await EnsureReviewerCanAccessAsync(detail, ct);
        var (bytes, fileName, contentType) = await _service.DownloadDocumentAsync(id, ct);
        return File(bytes, contentType, fileName);
    }

    /// <summary>Historial completo de cambios de estado de una solicitud.</summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ResignationRetirementStatusHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromRoute] int id, CancellationToken ct)
    {
        var detail = await _service.GetDetailByIdAsync(id, ct);
        await EnsureReviewerCanAccessAsync(detail, ct);
        return Ok(detail.History);
    }
}
