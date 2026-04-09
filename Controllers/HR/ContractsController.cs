using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.ContractRequest;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractsService _service;
    private readonly IMapper _mapper;

    public ContractsController(IContractsService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    /// <summary>Lista todos los contratos.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ContractsDto>>(await _service.GetAllAsync(ct)));

    /// <summary>Retorna un resultado paginado de contratos.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
    /// <param name="sortBy">Campo de ordenamiento (opcional).</param>
    /// <param name="sortDirection">Dirección del orden: asc | desc (opcional).</param>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        System.Linq.Expressions.Expression<Func<Contracts, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            predicate = c => c.ContractCode.ToLower().Contains(term) || (c.ContractDescription != null && c.ContractDescription.ToLower().Contains(term));
        }

        var pagedEntities = predicate is not null
           ? await _service.GetPagedAsync(predicate, page, pageSize, ct)
           : await _service.GetPagedAsync(page, pageSize, ct);

        var dtoItems = _mapper.Map<List<ContractsDto>>(pagedEntities.Items);

        return Ok(new
        {
            //items = pagedEntities.Items,
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
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var entity = await _service.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        return Ok(_mapper.Map<ContractsDto>(entity));
    }

    /// <summary>Crea un nuevo contrato.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(ContractsCreateDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<Contracts>(dto);
        var created = await _service.CreateAsync(entity, ct);
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.ContractID },
            _mapper.Map<ContractsDto>(created)
        );
    }

    /// <summary>Actualiza un contrato existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ContractsUpdateDto dto, CancellationToken ct)
    {
        if (dto.ContractID != 0 && dto.ContractID != id)
            return BadRequest("ContractID no coincide con la ruta.");

        await _service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>Elimina un contrato por ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
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
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ContractChangeStatusDto dto, CancellationToken ct)
    {
        await _service.ChangeStatusAsync(id, dto.ToStatusTypeID, dto.Comment, ct);
        return NoContent();
    }

    /// <summary>Obtiene el historial de estados de un contrato.</summary>
    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> History(int id, CancellationToken ct)
    {
        var items = await _service.GetStatusHistoryAsync(id, ct);
        return Ok(items);
    }

    /// <summary>Obtiene los addendums de un contrato.</summary>
    [HttpGet("{id:int}/addendums")]
    public async Task<IActionResult> Addendums(int id, CancellationToken ct)
    {
        var items = await _service.GetAddendumsAsync(id, ct);
        return Ok(items);
    }
}
