using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.FinancialCertification;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("financial-certification")]
public class FinancialCertificationController : ControllerBase
{
    private readonly IFinancialCertificationService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _user;
    private readonly ILogger<FinancialCertificationController> _logger;

    public FinancialCertificationController(
        IFinancialCertificationService svc,
        IMapper mapper,
        ICurrentUserService userService,
        ILogger<FinancialCertificationController> logger)
    {
        _svc    = svc;
        _mapper = mapper;
        _user   = userService;
        _logger = logger;
    }

    /// <summary>Lista todos los registros de FinancialCertification.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<FinancialCertificationDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Retorna certificaciones paginadas con filtros opcionales.</summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? statusName,
        [FromQuery] int? requestId,
        [FromQuery] string? certCode,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new FinancialCertificationQueryFilter(statusName?.ToUpperInvariant().Trim(), requestId, certCode, search, page, pageSize);
        var result = await _svc.GetPagedAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>Retorna certificaciones en estado PENDIENTE_REVISION (buzón de Financiero).</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var items = await _svc.GetPendingAsync(ct);
        return Ok(items);
    }

    /// <summary>Obtiene un registro por ID con datos enriquecidos (statusName, requestSummary).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var detail = await _svc.GetDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Crea un nuevo registro. El estado inicial se asigna automáticamente.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FinancialCertificationCreateDto dto, CancellationToken ct)
    {
        _logger.LogInformation("Creando certificación financiera por empleado {EmployeeId}", _user.EmployeeId);

        var entityObj      = _mapper.Map<FinancialCertification>(dto);
        entityObj.CreatedAt = DateTime.Now;
        entityObj.CreatedBy = _user.IsAuthenticated ? _user.EmployeeId : null;

        var created = await _svc.CreateAsync(entityObj, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.CertificationId }, _mapper.Map<FinancialCertificationDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] FinancialCertificationUpdateDto dto, CancellationToken ct)
    {
        var entityObj      = _mapper.Map<FinancialCertification>(dto);
        entityObj.UpdatedAt = DateTime.Now;
        entityObj.UpdatedBy = _user.IsAuthenticated ? _user.EmployeeId : null;

        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Aprueba una certificación y marca la solicitud como PENDIENTE_CONTRATACION.</summary>
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve([FromRoute] int id, CancellationToken ct)
    {
        var userId = _user.EmployeeId ?? 0;
        await _svc.ApproveAsync(id, userId, ct);
        return NoContent();
    }

    /// <summary>Rechaza una certificación y marca la solicitud como CERT_RECHAZADA.</summary>
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject([FromRoute] int id, [FromBody] FinancialCertificationRejectDto dto, CancellationToken ct)
    {
        var userId = _user.EmployeeId ?? 0;
        await _svc.RejectAsync(id, dto.Reason, userId, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
