using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.ContractRequest;
using WsUtaSystem.Models;
using WsUtaSystem.Application.DTOs.Common;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("cv/contract-request")]
public class ContractRequestController : ControllerBase
{
    private readonly IContractRequestService _svc;
    private readonly IMapper _mapper;

    public ContractRequestController(IContractRequestService svc, IMapper mapper)
    {
        _svc    = svc;
        _mapper = mapper;
    }

    /// <summary>Lista todos los registros de ContractRequest.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ContractRequestDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Retorna solicitudes paginadas con filtros opcionales.</summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? statusName,
        [FromQuery] int? departmentId,
        [FromQuery] int? workModalityId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new ContractRequestQueryFilter(statusName?.ToUpperInvariant().Trim(), departmentId, workModalityId, search, page, pageSize);
        var result = await _svc.GetPagedAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>Retorna solicitudes filtradas por nombre de estado (e.g. PENDIENTE_CERT_FINANCIERA).</summary>
    [HttpGet("by-status")]
    public async Task<IActionResult> GetByStatus([FromQuery] string statusName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(statusName))
            return BadRequest("statusName es requerido.");

        var items = await _svc.GetByStatusAsync(statusName.ToUpperInvariant().Trim(), ct);
        return Ok(items);
    }

    /// <summary>Obtiene un registro por ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<ContractRequestDto>(e));
    }

    /// <summary>Retorna la cantidad de personas pendientes de contratar para una solicitud.</summary>
    [HttpGet("{id:int}/pending-count")]
    public async Task<IActionResult> GetPendingCount([FromRoute] int id, CancellationToken ct)
    {
        var count = await _svc.GetPendingCountAsync(id, ct);
        return Ok(new { requestId = id, pendingCount = count });
    }

    /// <summary>Crea un nuevo registro. El estado inicial se asigna automáticamente.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContractRequestCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<ContractRequest>(dto);
        var created   = await _svc.CreateAsync(entityObj, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.RequestId }, _mapper.Map<ContractRequestDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ContractRequestUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<ContractRequest>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
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
