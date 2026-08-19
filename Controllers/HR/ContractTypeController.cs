using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using WsUtaSystem.Application.DTOs.ContractType;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;


[ApiController]
[Route("contract-type")]
public class ContractTypeController : ControllerBase
{
    private readonly IContractTypeService _svc;
    private readonly IMapper _mapper;
    public ContractTypeController(IContractTypeService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de ContractType.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ContractTypeDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<ContractTypeDto>(e));
    }

    /// <summary>Retorna un resultado paginado de tipos de contrato.</summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
    /// <param name="search">Texto de búsqueda por nombre, código o descripción.</param>
    /// <param name="status">Filtra por estado ("1" activo, "0" inactivo). Null = todos.</param>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var hasStatus = !string.IsNullOrWhiteSpace(status);

        Expression<Func<ContractType, bool>>? predicate = null;

        if (hasSearch && hasStatus)
        {
            var term = search!.Trim().ToLower();
            predicate = c => c.Status == status &&
                (c.Name.ToLower().Contains(term) ||
                 (c.ContractCode != null && c.ContractCode.ToLower().Contains(term)) ||
                 (c.Description != null && c.Description.ToLower().Contains(term)));
        }
        else if (hasSearch)
        {
            var term = search!.Trim().ToLower();
            predicate = c =>
                c.Name.ToLower().Contains(term) ||
                (c.ContractCode != null && c.ContractCode.ToLower().Contains(term)) ||
                (c.Description != null && c.Description.ToLower().Contains(term));
        }
        else if (hasStatus)
        {
            predicate = c => c.Status == status;
        }

        var pagedEntities = predicate is not null
            ? await _svc.GetPagedAsync(predicate, page, pageSize, ct)
            : await _svc.GetPagedAsync(page, pageSize, ct);

        var dtoItems = _mapper.Map<List<ContractTypeDto>>(pagedEntities.Items);

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

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("CATALOGS.CREATE")]
    public async Task<IActionResult> Create([FromBody] ContractTypeCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<ContractType>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<ContractTypeDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ContractTypeUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<ContractType>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("CATALOGS.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Obtiene un tipo de contrato con la información de su plantilla documental por defecto.
    /// </summary>
    [HttpGet("{id:int}/template")]
    [ProducesResponseType(typeof(ContractTypeWithTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWithTemplate([FromRoute] int id, CancellationToken ct)
    {
        var result = await _svc.GetWithDefaultTemplateAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Asigna o quita la plantilla documental por defecto de un tipo de contrato.
    /// Enviar <c>templateId: null</c> para desvincular la plantilla.
    /// </summary>
    [HttpPatch("{id:int}/default-template")]
    [RequirePermission("CATALOGS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultTemplate(
        [FromRoute] int id,
        [FromBody] SetDefaultTemplateRequest request,
        CancellationToken ct)
    {
        await _svc.SetDefaultTemplateAsync(id, request.TemplateId, ct);
        return NoContent();
    }

    /// <summary>
    /// Asigna o quita la plantilla de delegación de un tipo de contrato (usada cuando
    /// Contracts.IsDelegation = true). Enviar <c>templateId: null</c> para desvincular.
    /// </summary>
    [HttpPatch("{id:int}/delegation-template")]
    [RequirePermission("CATALOGS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDelegationTemplate(
        [FromRoute] int id,
        [FromBody] SetDelegationTemplateRequest request,
        CancellationToken ct)
    {
        await _svc.SetDelegationTemplateAsync(id, request.TemplateId, ct);
        return NoContent();
    }

    /// <summary>
    /// Genera y reserva el siguiente número de documento para el tipo de contrato indicado.
    /// El número tiene el formato {prefix}-{year}-{seq:D3} (ej: CONT-OCAS-2026-001).
    /// </summary>
    [HttpPost("{id:int}/next-number")]
    [ProducesResponseType(typeof(ContractNextNumberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NextNumber([FromRoute] int id, CancellationToken ct)
    {
        var result = await _svc.GetNextNumberAsync(id, ct);
        return Ok(result);
    }
}
