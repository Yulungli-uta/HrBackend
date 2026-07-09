using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.EmployeeCertificate;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Certificados laborales del empleado autenticado. EmployeeId siempre resuelto desde
/// <see cref="ICurrentUserService"/> — nunca aceptado desde el frontend.
/// </summary>
[ApiController]
[Route("employee-self-service/certificates")]
public sealed class EmployeeCertificatesController : ControllerBase
{
    private const string ModuleCode = "EMPLOYEE_CERTIFICATE_REQUESTS";

    private readonly IEmployeeCertificateService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserAccessScopeService _accessScopeService;
    private readonly ILogger<EmployeeCertificatesController> _logger;

    public EmployeeCertificatesController(
        IEmployeeCertificateService service,
        ICurrentUserService currentUser,
        IUserAccessScopeService accessScopeService,
        ILogger<EmployeeCertificatesController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _accessScopeService = accessScopeService;
        _logger = logger;
    }

    private int RequireEmployeeId()
        => _currentUser.EmployeeId
           ?? throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

    private async Task EnsureReviewerCanAccessAsync(EmployeeCertificateDetailDto detail, CancellationToken ct)
    {
        var reviewerId = RequireEmployeeId();
        if (detail.DepartmentId is null) return;
        await _accessScopeService.EnsureDepartmentAllowedAsync(reviewerId, ModuleCode, detail.DepartmentId.Value, ct);
    }

    // ── Mis certificados ──────────────────────────────────────────────────────

    [HttpPost("my")]
    [ProducesResponseType(typeof(EmployeeCertificateDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMy([FromBody] CreateEmployeeCertificateRequest request, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var result = await _service.CreateAsync(employeeId, request, ct);

        _logger.LogInformation("Certificado {CertificateType} emitido (RequestId={RequestId}) para EmployeeId={EmployeeId}",
            request.CertificateType, result.RequestId, employeeId);

        return CreatedAtAction(nameof(GetMyById), new { id = result.RequestId }, result);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedEmployeeCertificateResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy([FromQuery] EmployeeCertificateQueryFilter filter, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        return Ok(await _service.GetMyRequestsAsync(employeeId, filter, ct));
    }

    [HttpGet("my/{id:int}")]
    [ProducesResponseType(typeof(EmployeeCertificateDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyById([FromRoute] int id, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        return Ok(await _service.GetMyRequestDetailAsync(id, employeeId, ct));
    }

    [HttpGet("my/{id:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadMy([FromRoute] int id, CancellationToken ct)
    {
        var employeeId = RequireEmployeeId();
        var (bytes, fileName, contentType) = await _service.DownloadMyDocumentAsync(id, employeeId, ct);
        return File(bytes, contentType, fileName);
    }

    // ── Recursos Humanos ──────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(PagedEmployeeCertificateResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] EmployeeCertificateQueryFilter filter, CancellationToken ct)
    {
        var allowedDeptIds = _currentUser.EmployeeId.HasValue
            ? await _accessScopeService.GetAllowedDepartmentIdsAsync(_currentUser.EmployeeId.Value, ModuleCode, ct)
            : null;
        var effectiveFilter = allowedDeptIds is null ? filter : filter with { AllowedDepartmentIds = allowedDeptIds };
        return Ok(await _service.GetPagedAsync(effectiveFilter, ct));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeCertificateDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _service.GetDetailByIdAsync(id, ct);
        await EnsureReviewerCanAccessAsync(result, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download([FromRoute] int id, CancellationToken ct)
    {
        var detail = await _service.GetDetailByIdAsync(id, ct);
        await EnsureReviewerCanAccessAsync(detail, ct);
        var (bytes, fileName, contentType) = await _service.DownloadAsync(id, ct);
        return File(bytes, contentType, fileName);
    }
}
