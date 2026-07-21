using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.ContractRequest;
using WsUtaSystem.Application.DTOs.ContractRequestPerson;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("contract-request")]
public class ContractRequestController : ControllerBase
{
    private readonly IContractRequestService _svc;
    private readonly IContractRequestPersonService _personSvc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _user;

    public ContractRequestController(
        IContractRequestService svc,
        IContractRequestPersonService personSvc,
        IMapper mapper,
        ICurrentUserService user)
    {
        _svc       = svc;
        _personSvc = personSvc;
        _mapper    = mapper;
        _user      = user;
    }

    /// <summary>Lista todos los registros de ContractRequest.</summary>
    [HttpGet]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ContractRequestDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Retorna solicitudes paginadas con filtros opcionales.</summary>
    [HttpGet("paged")]
    [RequirePermission("CONTRACTS.READ")]
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
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetByStatus([FromQuery] string statusName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(statusName))
            return BadRequest("statusName es requerido.");

        var items = await _svc.GetByStatusAsync(statusName.ToUpperInvariant().Trim(), ct);
        return Ok(items);
    }

    /// <summary>Obtiene un registro por ID.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<ContractRequestDto>(e));
    }

    /// <summary>Retorna la cantidad de personas pendientes de contratar para una solicitud.</summary>
    [HttpGet("{id:int}/pending-count")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetPendingCount([FromRoute] int id, CancellationToken ct)
    {
        var count = await _svc.GetPendingCountAsync(id, ct);
        return Ok(new { requestId = id, pendingCount = count });
    }

    /// <summary>Crea un nuevo registro. El estado inicial se asigna automáticamente.</summary>
    [HttpPost]
    [RequirePermission("CONTRACTS.CREATE")]
    public async Task<IActionResult> Create([FromBody] ContractRequestCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<ContractRequest>(dto);
        var created   = await _svc.CreateAsync(entityObj, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.RequestId }, _mapper.Map<ContractRequestDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CONTRACTS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ContractRequestUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<ContractRequest>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("CONTRACTS.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    // ── Personas del detalle de solicitud ────────────────────────────────────

    /// <summary>Retorna todas las personas registradas en el detalle de una solicitud.</summary>
    [HttpGet("{id:int}/people")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetPeople([FromRoute] int id, CancellationToken ct)
    {
        var items = await _personSvc.GetByRequestAsync(id, ct);
        return Ok(items);
    }

    /// <summary>Retorna las personas pendientes (PENDIENTE) del detalle de una solicitud.</summary>
    [HttpGet("{id:int}/pending-people")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetPendingPeople([FromRoute] int id, CancellationToken ct)
    {
        var items = await _personSvc.GetPendingByRequestAsync(id, ct);
        return Ok(items);
    }

    /// <summary>Retorna información de cupos (contratados, libres, pendientes).</summary>
    [HttpGet("{id:int}/slots")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetSlots([FromRoute] int id, CancellationToken ct)
    {
        var slots = await _svc.GetSlotsAsync(id, ct);
        return Ok(slots);
    }

    /// <summary>Busca personas disponibles para vincular a una solicitud.</summary>
    [HttpGet("{id:int}/available-people")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetAvailablePeople(
        [FromRoute] int id,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var people = await _svc.SearchAvailablePeopleAsync(id, search, ct);
        return Ok(people);
    }

    /// <summary>Agrega una persona al detalle de una solicitud.</summary>
    [HttpPost("{id:int}/people")]
    [RequirePermission("CONTRACTS.UPDATE")]
    public async Task<IActionResult> AddPerson(
        [FromRoute] int id,
        [FromBody] CreateContractRequestPersonDto dto,
        CancellationToken ct)
    {
        var userId  = _user.EmployeeId ?? 0;
        var created = await _personSvc.AddPersonAsync(id, dto, userId, ct);
        return CreatedAtAction(nameof(GetPeople), new { id }, created);
    }

    /// <summary>Actualiza los datos de una persona en el detalle.</summary>
    [HttpPut("{id:int}/people/{personId:int}")]
    [RequirePermission("CONTRACTS.UPDATE")]
    public async Task<IActionResult> UpdatePerson(
        [FromRoute] int id,
        [FromRoute] int personId,
        [FromBody] UpdateContractRequestPersonDto dto,
        CancellationToken ct)
    {
        var userId = _user.EmployeeId ?? 0;
        await _personSvc.UpdatePersonAsync(personId, dto, userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Marca una persona del detalle como contratada (llamado tras crear el contrato).
    /// </summary>
    [HttpPost("{id:int}/people/{personId:int}/generate-contract")]
    [RequirePermission("CONTRACTS.APPROVE")]
    public async Task<IActionResult> GenerateContractFromPerson(
        [FromRoute] int id,
        [FromRoute] int personId,
        [FromBody] HireRequestPersonDto dto,
        CancellationToken ct)
    {
        var userId = _user.EmployeeId ?? 0;
        await _personSvc.HireAsync(personId, dto.ContractId, userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Registra la contratación de una persona disponible (no estaba en el detalle).
    /// </summary>
    [HttpPost("{id:int}/available-people/generate-contract")]
    [RequirePermission("CONTRACTS.APPROVE")]
    public async Task<IActionResult> GenerateContractFromAvailablePerson(
        [FromRoute] int id,
        [FromBody] GenerateContractFromAvailablePersonDto dto,
        CancellationToken ct)
    {
        var userId = _user.EmployeeId ?? 0;
        await _personSvc.RecordHiredFromAvailableAsync(id, dto.PersonId, dto.JobId, dto.ContractId, userId, ct);
        return NoContent();
    }

    /// <summary>Envía la solicitud a estado PENDIENTE_CORRECCION.</summary>
    [HttpPost("{id:int}/send-to-correction")]
    [RequirePermission("CONTRACTS.UPDATE")]
    public async Task<IActionResult> SendToCorrection(
        [FromRoute] int id,
        [FromBody] RejectTemporaryDto dto,
        CancellationToken ct)
    {
        var userId = _user.EmployeeId ?? 0;
        await _svc.SendToCorrectionAsync(id, dto.Reason ?? string.Empty, userId, ct);
        return NoContent();
    }
}
