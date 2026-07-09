using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.EmployeeInternalRequest;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Solicitudes internas del empleado autenticado (actualización de datos, documentos,
/// información, otros). EmployeeId siempre resuelto desde <see cref="ICurrentUserService"/>.
/// </summary>
[ApiController]
[Route("employee-self-service/internal-requests")]
public sealed class EmployeeInternalRequestsController : ControllerBase
{
    private const string ModuleCode = "EMPLOYEE_INTERNAL_REQUESTS";

    private readonly IEmployeeInternalRequestService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserAccessScopeService _accessScopeService;
    private readonly ILogger<EmployeeInternalRequestsController> _logger;

    public EmployeeInternalRequestsController(
        IEmployeeInternalRequestService service,
        ICurrentUserService currentUser,
        IUserAccessScopeService accessScopeService,
        ILogger<EmployeeInternalRequestsController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _accessScopeService = accessScopeService;
        _logger = logger;
    }

    private int RequireEmployeeId()
        => _currentUser.EmployeeId
           ?? throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

    private async Task EnsureReviewerCanAccessAsync(EmployeeInternalRequestDetailDto detail, CancellationToken ct)
    {
        var reviewerId = RequireEmployeeId();
        if (detail.DepartmentId is null) return;
        await _accessScopeService.EnsureDepartmentAllowedAsync(reviewerId, ModuleCode, detail.DepartmentId.Value, ct);
    }

    // ── Mis solicitudes ───────────────────────────────────────────────────────

    [HttpPost("my")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMy([FromBody] CreateEmployeeInternalRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _service.CreateAsync(employeeId, request, ct);
        _logger.LogInformation("Solicitud interna {RequestType} creada (RequestId={RequestId}) por EmployeeId={EmployeeId}",
            request.RequestType, result.RequestId, employeeId);
        return CreatedAtAction(nameof(GetMyById), new { id = result.RequestId }, result);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedEmployeeInternalRequestResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy([FromQuery] EmployeeInternalRequestQueryFilter filter, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        return Ok(await _service.GetMyRequestsAsync(employeeId, filter, ct));
    }

    [HttpGet("my/{id:int}")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyById([FromRoute] int id, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        return Ok(await _service.GetMyRequestDetailAsync(id, employeeId, ct));
    }

    [HttpPut("my/{id:int}")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMy([FromRoute] int id, [FromBody] UpdateEmployeeInternalRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        return Ok(await _service.UpdateAsync(id, employeeId, request, ct));
    }

    [HttpPost("my/{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelMy([FromRoute] int id, [FromBody] CancelEmployeeInternalRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        await _service.CancelOwnAsync(id, employeeId, request, ct);
        return NoContent();
    }

    // ── Recursos Humanos ──────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(PagedEmployeeInternalRequestResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] EmployeeInternalRequestQueryFilter filter, CancellationToken ct)
    {
        var allowedDeptIds = _currentUser.EmployeeId.HasValue
            ? await _accessScopeService.GetAllowedDepartmentIdsAsync(_currentUser.EmployeeId.Value, ModuleCode, ct)
            : null;
        var effectiveFilter = allowedDeptIds is null ? filter : filter with { AllowedDepartmentIds = allowedDeptIds };
        return Ok(await _service.GetPagedAsync(effectiveFilter, ct));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _service.GetDetailByIdAsync(id, ct);
        await EnsureReviewerCanAccessAsync(result, ct);
        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve([FromRoute] int id, [FromBody] ReviewEmployeeInternalRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var reviewedBy = RequireEmployeeId();
        return Ok(await _service.ApproveAsync(id, reviewedBy, request, ct));
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject([FromRoute] int id, [FromBody] ReviewEmployeeInternalRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var reviewedBy = RequireEmployeeId();
        return Ok(await _service.RejectAsync(id, reviewedBy, request, ct));
    }

    [HttpPost("{id:int}/return")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Return([FromRoute] int id, [FromBody] ReviewEmployeeInternalRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var reviewedBy = RequireEmployeeId();
        return Ok(await _service.ReturnAsync(id, reviewedBy, request, ct));
    }

    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete([FromRoute] int id, [FromBody] ReviewEmployeeInternalRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var resolvedBy = RequireEmployeeId();
        return Ok(await _service.CompleteAsync(id, resolvedBy, request, ct));
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(EmployeeInternalRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel([FromRoute] int id, [FromBody] CancelEmployeeInternalRequest request, CancellationToken ct)
    {
        await EnsureReviewerCanAccessAsync(await _service.GetDetailByIdAsync(id, ct), ct);
        var cancelledBy = RequireEmployeeId();
        return Ok(await _service.HrCancelAsync(id, cancelledBy, request, ct));
    }
}
